using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Auth.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Email;
using Eduva.Infrastructure.Identity.Interfaces;
using Eduva.Infrastructure.Identity.Providers;
using Eduva.Infrastructure.Services;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Eduva.Infrastructure.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtHandler _jwtHandler;
        private readonly ITokenBlackListService _tokenBlackListService;
        private readonly IServiceProvider _serviceProvider;

        public AuthService(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ILogger<AuthService> logger,
            JwtHandler jwtHandler, ITokenBlackListService tokenBlackListService, IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _jwtHandler = jwtHandler;
            _tokenBlackListService = tokenBlackListService;
            _serviceProvider = serviceProvider;
        }

        public async Task<CustomCode> RegisterAsync(RegisterRequestDto request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Attempt to register with existing email: {Email}.", request.Email);
                throw new EmailAlreadyExistsException();
            }

            var userId = Guid.NewGuid();

            var newUser = new ApplicationUser
            {
                Id = userId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                UserName = request.Email,
                FullName = request.FullName,
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to create user. Errors: {Errors}", string.Join(", ", errors));
                throw new AppException(CustomCode.ProvidedInformationIsInValid, errors);
            }

            await _userManager.AddToRoleAsync(newUser, nameof(Role.SchoolAdmin));

            _ = SendConfirmEmailMessage(request.ClientUrl, newUser);

            return CustomCode.ConfirmationEmailSent;
        }

        private async Task SendConfirmEmailMessage(string clientUrl, ApplicationUser newUser)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var verifyLink = $"{clientUrl}?email={Uri.EscapeDataString(newUser.Email!)}&token={Uri.EscapeDataString(token)}";

            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", "verify-email.html");

            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template file not found at path: {Path}", templatePath);
                throw new FileNotFoundException("Template file not found", templatePath);
            }

            var template = await File.ReadAllTextAsync(templatePath);

            var htmlBody = template
                 .Replace("{{verify_link}}", verifyLink)
                 .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());


            var message = new EmailMessage(
                    [new(newUser.Email!, newUser.FullName ?? newUser.Email!)],
                    "Xác Minh Địa Chỉ Email",
                    htmlBody,
                    null
                );

            _logger.LogInformation("Sending email to '{Email}' to confirm email.", newUser.Email);

            //_ = _emailSender.SendEmailBrevoAsync(newUser.Email!, newUser.FirstName + " " + newUser.LastName, message.Subject, message.Content);

            _ = _emailSender.SendEmailAsync(message);
        }

        public async Task<(CustomCode, AuthResultDto)> LoginAsync(LoginRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Attempt to login with unconfirmed email: {Email}.", request.Email);
                throw new UserNotConfirmedException();
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Attempt to login with invalid password for email: {Email}.", request.Email);
                throw new InvalidCredentialsException();
            }

            // Check if user account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Attempt to login with locked account for email: {Email}.", request.Email);
                throw new UserAccountLockedException();
            }

            if (await _userManager.GetTwoFactorEnabledAsync(user))
            {
                var otp = await GetOtpProvider().GenerateAsync("OTP", _userManager, user);

                await SendOtpEmailMessage(user, otp);

                return (CustomCode.RequiresOtpVerification, new AuthResultDto
                {
                    Requires2FA = true,
                    Email = user.Email!
                });
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var userClaims = await _userManager.GetClaimsAsync(user);

            if (user.SchoolId.HasValue)
            {
                userClaims.Add(new Claim("SchoolId", user.SchoolId.Value.ToString()));
            }

            var authResponse = await GenerateToken(user, userRoles, userClaims, true);

            return (CustomCode.Success, authResponse);
        }

        private async Task SendOtpEmailMessage(ApplicationUser user, string otp)
        {
            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", "otp-verification.html");

            if (!File.Exists(templatePath))
            {
                _logger.LogError("OTP email template not found at path: {Path}", templatePath);
                throw new FileNotFoundException("Template file not found", templatePath);
            }

            var template = await File.ReadAllTextAsync(templatePath);

            var htmlBody = template
                .Replace("{{otp_code}}", otp)
                .Replace("{{current_year}}", DateTime.UtcNow.Year.ToString());

            var message = new EmailMessage(
                [new EmailAddress(user.Email!, user.FullName ?? user.Email!)],
                "Xác thực OTP đăng nhập",
                htmlBody,
                null
            );

            await _emailSender.SendEmailAsync(message);
        }

        public async Task<(CustomCode, AuthResultDto)> VerifyLoginOtpAsync(VerifyOtpRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                throw new AppException(CustomCode.Forbidden);
            }

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "OTP", request.OtpCode);
            if (!isValid)
            {
                throw new OtpInvalidOrExpireException();
            }

            await GetOtpProvider().ForceClearOtpClaimsAsync(_userManager, user);

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var authResponse = await GenerateToken(user, roles, claims, true);

            return (CustomCode.Success, authResponse);
        }

        private async Task Send2FaOtpEmailAsync(ApplicationUser user, string subject)
        {
            var otp = await GetOtpProvider().GenerateAsync("OTP", _userManager, user);

            var message = await MailMessageHelper.CreateMessageAsync(user, otp, subject);

            await _emailSender.SendEmailAsync(message);
        }

        private async Task<CustomCode> Confirm2FaChangeAsync(ApplicationUser user, string otpCode, bool enable)
        {
            var isValidOtp = await _userManager.VerifyTwoFactorTokenAsync(user, "OTP", otpCode);
            if (!isValidOtp)
            {
                throw new OtpInvalidOrExpireException();
            }

            user.TwoFactorEnabled = enable;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new AppException(CustomCode.ProvidedInformationIsInValid, errors);
            }

            await GetOtpProvider().ForceClearOtpClaimsAsync(_userManager, user);

            return CustomCode.Success;
        }

        public async Task<CustomCode> RequestEnable2FaOtpAsync(Request2FaDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            if (user.TwoFactorEnabled)
            {
                throw new TwoFactorIsAlreadyEnabledException();
            }

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                throw new InvalidCredentialsException();
            }

            await Send2FaOtpEmailAsync(user, "Bật xác thực 2 yếu tố - EDUVA");

            return CustomCode.OtpSentSuccessfully;
        }

        public async Task<CustomCode> ConfirmEnable2FaOtpAsync(Confirm2FaDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            if (user.TwoFactorEnabled)
            {
                throw new TwoFactorIsAlreadyEnabledException();
            }

            return await Confirm2FaChangeAsync(user, request.OtpCode, true);
        }
        public async Task<CustomCode> RequestDisable2FaOtpAsync(Request2FaDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            if (!user.TwoFactorEnabled)
            {
                throw new TwoFactorIsAlreadyDisabledException();
            }

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                throw new InvalidCredentialsException();
            }

            await Send2FaOtpEmailAsync(user, "Tắt xác thực 2 yếu tố - EDUVA");

            return CustomCode.OtpSentSuccessfully;
        }
        public async Task<CustomCode> ConfirmDisable2FaOtpAsync(Confirm2FaDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            if (!user.TwoFactorEnabled)
            {
                throw new TwoFactorIsAlreadyDisabledException();
            }

            return await Confirm2FaChangeAsync(user, request.OtpCode, false);
        }

        private async Task<AuthResultDto> GenerateToken(ApplicationUser user, IList<string> roles, IList<Claim> userClaims, bool populateExp)
        {
            var signingCredentials = _jwtHandler.GetSigningCredentials();
            var claims = TokenHelper.GetClaims(user, roles, userClaims);

            var tokenOptions = _jwtHandler.GenerateTokenOptions(signingCredentials, claims);

            var refreshToken = TokenHelper.GenerateRefreshToken();

            user.RefreshToken = refreshToken;

            if (populateExp)
            {
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            }

            await _userManager.UpdateAsync(user);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return new AuthResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtHandler.GetExpiryInSecond(),
                Requires2FA = false,
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? user.Email
            };
        }

        public async Task<CustomCode> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var message = MailMessageHelper.CreateMessage(user, token, request.ClientUrl, "reset-password.html", "Đặt Lại Mật Khẩu");

            //_ = _emailSender.SendEmailBrevoAsync(user.Email!, user.FirstName + " " + user.LastName, message.Subject, message.Content);

            _ = _emailSender.SendEmailAsync(message);

            return CustomCode.ResetPasswordEmailSent;
        }

        public async Task<CustomCode> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();

            if (await _userManager.CheckPasswordAsync(user, request.Password))
            {
                throw new NewPasswordSameAsOldException();
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to reset password. Errors: {Errors}", string.Join(", ", errors));
                throw new AppException(CustomCode.Unauthorized, errors);
            }

            return CustomCode.PasswordResetSuccessful;
        }

        public async Task<CustomCode> ConfirmEmailAsync(ConfirmEmailRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to confirm email. Errors: {Errors}", string.Join(", ", errors));
                throw new AppException(CustomCode.ConfirmEmailTokenInvalidOrExpired, errors);
            }

            return CustomCode.Success;
        }

        public async Task<CustomCode> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new UserNotExistsException();

            if (user.EmailConfirmed)
            {
                _logger.LogWarning("Attempt to resend confirmation email for already confirmed email: {Email}.", request.Email);
                throw new UserAlreadyConfirmedException();
            }

            _ = SendConfirmEmailMessage(request.ClientUrl, user);

            return CustomCode.ConfirmationEmailSent;
        }

        public async Task<(CustomCode, AuthResultDto)> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var principal = _jwtHandler.GetPrincipalFromExpiredToken(request.AccessToken);

            var username = principal.Identity?.Name ?? throw new InvalidTokenException();

            var user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new InvalidTokenException(["Refresh token is invalid or expired."]);
            }

            // Check if user account is locked
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Attempt to login with locked account for email: {Email}.", user.Email);
                throw new UserAccountLockedException();
            }

            var previousTokenExpiry = TokenHelper.GetTokenExpiry(request.AccessToken);
            if (previousTokenExpiry > DateTime.UtcNow)
            {
                await _tokenBlackListService.BlacklistTokenAsync(request.AccessToken, previousTokenExpiry);
                _logger.LogInformation("Previous access token invalidated for user: {Username}", username);
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var userClaims = await _userManager.GetClaimsAsync(user);

            var authResponse = await GenerateToken(user, userRoles, userClaims, false);

            return (CustomCode.Success, authResponse);
        }

        public async Task LogoutAsync(string userId, string accessToken)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                user.RefreshToken = null!;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);
            }

            var expiry = TokenHelper.GetTokenExpiry(accessToken);

            if (expiry > DateTime.UtcNow)
            {
                await _tokenBlackListService.BlacklistTokenAsync(accessToken, expiry);
            }
        }

        public async Task<CustomCode> ChangePasswordAsync(ChangePasswordRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new UserNotExistsException();

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                throw new AppException(CustomCode.IncorrectCurrentPassword);
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                throw new NewPasswordSameAsOldException();
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError("Failed to change password. Errors: {Errors}", string.Join(", ", errors));
                throw new AppException(CustomCode.ProvidedInformationIsInValid, errors);
            }

            // Handle logout behavior
            switch (request.LogoutBehavior)
            {
                case LogoutBehavior.LogoutAllIncludingCurrent:
                    await _tokenBlackListService.BlacklistAllUserTokensAsync(request.UserId.ToString());
                    _logger.LogInformation("All tokens invalidated for user {UserId} after password change", request.UserId);
                    break;

                case LogoutBehavior.LogoutOthersOnly:
                    var accessToken = request.CurrentAccessToken;
                    await _tokenBlackListService.BlacklistAllUserTokensExceptAsync(request.UserId.ToString(), accessToken);
                    _logger.LogInformation("All tokens except current invalidated for user {UserId}", request.UserId);
                    break;

                default:
                    _logger.LogInformation("No session invalidated for user {UserId}", request.UserId);
                    break;
            }

            return CustomCode.Success;
        }

        public async Task InvalidateAllUserTokensAsync(string userId)
        {
            // Clear refresh token from database
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null!;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);
            }

            // Invalidate all tokens for this user
            await _tokenBlackListService.BlacklistAllUserTokensAsync(userId);
            _logger.LogInformation("All tokens invalidated for user {UserId}", userId);
        }

        public async Task<CustomCode> ResendOtpAsync(ResendOtpRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new UserNotExistsException();

            // Check resend throttle
            await EnsureOtpThrottleAsync(user);


            var subject = dto.Purpose switch
            {
                OtpPurpose.Login => "Xác thực OTP đăng nhập",
                OtpPurpose.Enable2FA => "Bật xác thực 2 yếu tố - EDUVA",
                OtpPurpose.Disable2FA => "Tắt xác thực 2 yếu tố - EDUVA",
                _ => "Xác thực OTP"
            };

            var otp = await GetOtpProvider().GenerateAsync("OTP", _userManager, user);

            var message = await MailMessageHelper.CreateMessageAsync(user, otp, subject);

            await _emailSender.SendEmailAsync(message);

            return CustomCode.OtpSentSuccessfully;
        }

        private async Task EnsureOtpThrottleAsync(ApplicationUser user)
        {
            var otpProvider = GetOtpProvider();
            if (otpProvider == null)
            {
                _logger.LogError("OTP Provider could not be resolved from service provider.");
                throw new InvalidOperationException("OTP Provider not available.");
            }

            await otpProvider.CheckOtpThrottleAsync(_userManager, user);
        }

        private SixDigitTokenProvider<ApplicationUser> GetOtpProvider()
        {
            return _serviceProvider.GetRequiredService<SixDigitTokenProvider<ApplicationUser>>();
        }
    }
}