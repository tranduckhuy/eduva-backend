using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Auth.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Identity;
using Eduva.Infrastructure.Identity.Interfaces;
using Eduva.Infrastructure.Identity.Providers;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _userManager = default!;
        private Mock<IEmailSender> _emailSender = default!;
        private Mock<ILogger<AuthService>> _logger = default!;
        private Mock<ITokenBlackListService> _tokenService = default!;
        private AuthService _authService = default!;
        private const string ValidClientUrl = "https://localhost:9001/api/auth/confirm-email";

        #region AuthServiceTests Setup and TearDown

        [SetUp]
        public void Setup()
        {
            _userManager = MockUserManager<ApplicationUser>();
            _emailSender = new Mock<IEmailSender>();
            _logger = new Mock<ILogger<AuthService>>();
            _tokenService = new Mock<ITokenBlackListService>();

            _userManager.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
               .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
                        .ReturnsAsync(IdentityResult.Success);

            _userManager.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(new List<Claim>());

            var jwtSettings = new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "super_secret_key_1234567890123456",
                ["JwtSettings:ValidIssuer"] = "issuer",
                ["JwtSettings:ValidAudience"] = "audience",
                ["JwtSettings:ExpiryInSecond"] = "3600"
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(jwtSettings)
                .Build();

            var jwtHandler = new JwtHandler(config, new Mock<ILogger<JwtHandler>>().Object);

            var otpLogger = new Mock<ILogger<SixDigitTokenProvider<ApplicationUser>>>();
            var otpOptions = Options.Create(new SixDigitTokenProviderOptions
            {
                TokenLifespan = TimeSpan.FromSeconds(120)
            });
            var otpProvider = new SixDigitTokenProvider<ApplicationUser>(otpOptions, otpLogger.Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(sp => sp.GetService(typeof(SixDigitTokenProvider<ApplicationUser>)))
                .Returns(otpProvider);

            _authService = new AuthService(
                _userManager.Object,
                _emailSender.Object,
                _logger.Object,
                jwtHandler,
                _tokenService.Object,
                serviceProvider.Object);

            CreateEmailTemplate("verify-email.html", "<html>{{verify_link}} {{current_year}}</html>");
            CreateEmailTemplate("otp-verification.html", "<html>Your code: {{otp_code}} {{current_year}}</html>");
            CreateEmailTemplate("reset-password.html", "<html>Reset: {{reset_link}} {{current_year}}</html>");
        }

        [TearDown]
        public void TearDown()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "email-templates");
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    File.Delete(file);
                }
            }
        }

        #endregion

        // Tests for RegisterAsync method - email exists, valid input, create user failed.

        #region RegisterAsync Tests

        [Test]
        public void RegisterAsync_EmailExists_ThrowsException()
        {
            _userManager.Setup(m => m.FindByEmailAsync("test@example.com"))
                .ReturnsAsync(new ApplicationUser());

            var dto = new RegisterRequestDto { Email = "test@example.com" };

            Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _authService.RegisterAsync(dto));
        }

        [Test]
        public async Task RegisterAsync_ValidInput_ReturnsSuccessCode()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("fake-token");

            var dto = new RegisterRequestDto
            {
                Email = "sang@gmail.com",
                Password = "Sangtran1309!",
                ConfirmPassword = "Sangtran1309!",
                FullName = "Tran Ngoc Sang",
                PhoneNumber = "0935323123",
                ClientUrl = ValidClientUrl
            };

            var result = await _authService.RegisterAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.ConfirmationEmailSent));
        }

        [Test]
        public void RegisterAsync_CreateUserFailed_ThrowsAppException()
        {
            // Arrange
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser?)null);

            var identityErrors = new List<IdentityError>
    {
        new IdentityError { Description = "Password too weak" },
        new IdentityError { Description = "Email is invalid" }
    };

            var failedResult = IdentityResult.Failed(identityErrors.ToArray());

            _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(failedResult);

            var dto = new RegisterRequestDto
            {
                Email = "fail@example.com",
                Password = "123",
                ConfirmPassword = "123",
                FullName = "Test",
                PhoneNumber = "0123456789",
                ClientUrl = "http://localhost"
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(dto));
            Assert.That(ex!.Errors, Does.Contain("Password too weak"));
            Assert.That(ex!.Errors, Does.Contain("Email is invalid"));
        }

        #endregion

        // Tests for LoginAsync method - user not found, email not confirmed, 2FA enabled, etc.

        #region LoginAsync Tests

        [Test]
        public void LoginAsync_UserNotExists_ThrowsException()
        {
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new LoginRequestDto { Email = "notfound@example.com", Password = "password" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.LoginAsync(dto));
        }

        [Test]
        public void LoginAsync_EmailNotConfirmed_ThrowsException()
        {
            var user = new ApplicationUser { EmailConfirmed = false };
            _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new LoginRequestDto { Email = "user@example.com", Password = "password" };

            Assert.ThrowsAsync<UserNotConfirmedException>(() => _authService.LoginAsync(dto));
        }

        [Test]
        public async Task LoginAsync_ShouldSend2FAToken_WhenFullNameIsNull()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                FullName = null,
                EmailConfirmed = true
            };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            _emailSender.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>())).Returns(Task.CompletedTask);

            var result = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = "user@example.com",
                Password = "P@ssword"
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.EqualTo(CustomCode.RequiresOtpVerification));
                Assert.That(result.Item2.Requires2FA, Is.True);
                Assert.That(result.Item2.Email, Is.EqualTo("user@example.com"));
            });

            _emailSender.Verify(x => x.SendEmailAsync(It.Is<EmailMessage>(m =>
                m.To.First().DisplayName == "user@example.com"
            )), Times.Once);
        }

        [Test]
        public async Task LoginAsync_2FAEnabled_ReturnsOtpRequired()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                EmailConfirmed = true,
                FullName = "User"
            };

            _userManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
            _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            _emailSender.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>())).Returns(Task.CompletedTask);

            var result = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = user.Email,
                Password = "password"
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.EqualTo(CustomCode.RequiresOtpVerification));
                Assert.That(result.Item2.Requires2FA, Is.True);
                Assert.That(result.Item2.Email, Is.EqualTo(user.Email));
            });

            _emailSender.Verify(x => x.SendEmailAsync(It.Is<EmailMessage>(m =>
                m.To.First().DisplayName == "User"
            )), Times.Once);
        }

        [Test]
        public async Task LoginAsync_SuccessWithout2FA_ReturnsAuthResult()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user@example.com",
                FullName = "Test User",
                EmailConfirmed = true,
                RefreshToken = "refresh-token",
                RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName!)
            });

            var dto = new LoginRequestDto
            {
                Email = user.Email,
                Password = "password"
            };

            var (code, result) = await _authService.LoginAsync(dto);

            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
                Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
                Assert.That(result.Requires2FA, Is.False);
            });
        }

        [Test]
        public void LoginAsync_InvalidPassword_ThrowsInvalidCredentialsException()
        {
            var user = new ApplicationUser { Email = "test@example.com", EmailConfirmed = true };

            _userManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var dto = new LoginRequestDto { Email = user.Email, Password = "wrong-password" };

            Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(dto));
        }

        [Test]
        public void LoginAsync_UserIsLockedOut_ThrowsUserAccountLockedException()
        {
            var user = new ApplicationUser { Email = "locked@example.com", EmailConfirmed = true };

            _userManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var dto = new LoginRequestDto { Email = user.Email, Password = "password" };

            Assert.ThrowsAsync<UserAccountLockedException>(() => _authService.LoginAsync(dto));
        }

        #endregion

        // Tests for VerifyLoginOtpAsync method - valid OTP, user not found, 2FA not enabled, invalid OTP.

        #region VerifyLoginOtpAsync Tests

        [Test]
        public async Task VerifyLoginOtpAsync_ValidOtp_ReturnsAuthResult()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user@example.com",
                FullName = "Test User",
                RefreshToken = "old-refresh",
                RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(1)
            };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, "OTP", "123456"))
                .ReturnsAsync(true);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName!)
            });

            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new VerifyOtpRequestDto
            {
                Email = user.Email,
                OtpCode = "123456"
            };

            var (code, result) = await _authService.VerifyLoginOtpAsync(dto);

            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty, "AccessToken should not be empty");
                Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty, "RefreshToken should not be empty");
                Assert.That(result.Email, Is.EqualTo(user.Email), "Email in AuthResultDto should match user.Email");
                Assert.That(result.Requires2FA, Is.False);
            });
        }

        [Test]
        public void VerifyLoginOtpAsync_UserNotExists_ThrowsException()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new VerifyOtpRequestDto { Email = "user@example.com", OtpCode = "123456" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.VerifyLoginOtpAsync(dto));
        }

        [Test]
        public void VerifyLoginOtpAsync_TwoFactorNotEnabled_ThrowsException()
        {
            var user = new ApplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);

            var dto = new VerifyOtpRequestDto { Email = "user@example.com", OtpCode = "123456" };

            Assert.ThrowsAsync<AppException>(() => _authService.VerifyLoginOtpAsync(dto));
        }

        [Test]
        public void VerifyLoginOtpAsync_InvalidOtp_ThrowsException()
        {
            var user = new ApplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            var dto = new VerifyOtpRequestDto { Email = "user@example.com", OtpCode = "wrong" };

            Assert.ThrowsAsync<OtpInvalidOrExpireException>(() => _authService.VerifyLoginOtpAsync(dto));
        }

        #endregion

        // Tests for ChangePasswordAsync method - user not found, invalid current password, etc.

        #region ChangePasswordAsync Tests

        [Test]
        public async Task ChangePasswordAsync_ShouldThrow_WhenUserNotFound()
        {
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequestDto
            {
                UserId = userId,
                CurrentPassword = "old",
                NewPassword = "new"
            };

            _userManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            try
            {
                await _authService.ChangePasswordAsync(request);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (UserNotExistsException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
            }
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIncorrect()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                        .ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "wrong"))
                        .ReturnsAsync(false);

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "wrong",
                NewPassword = "new"
            };

            try
            {
                await _authService.ChangePasswordAsync(request);
                Assert.Fail("Expected AppException was not thrown.");
            }
            catch (AppException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.IncorrectCurrentPassword));
            }
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldThrow_WhenNewPasswordSameAsOld()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                        .ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "same"))
                        .ReturnsAsync(true); // Current password

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "same",
                NewPassword = "same"
            };

            try
            {
                await _authService.ChangePasswordAsync(request);
                Assert.Fail("Expected NewPasswordSameAsOldException was not thrown.");
            }
            catch (NewPasswordSameAsOldException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldThrow_WhenChangePasswordFails()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                        .ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "old"))
                        .ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(user, "old", "new"))
                        .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Something went wrong" }));

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "old",
                NewPassword = "new"
            };

            try
            {
                await _authService.ChangePasswordAsync(request);
                Assert.Fail("Expected AppException was not thrown.");
            }
            catch (AppException ex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
                    Assert.That(ex.Errors, Contains.Item("Something went wrong"));
                });
            }
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldLogoutAllSessions_WhenConfigured()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "old")).ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "old",
                NewPassword = "new",
                LogoutBehavior = LogoutBehavior.LogoutAllIncludingCurrent
            };

            var result = await _authService.ChangePasswordAsync(request);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
            _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(user.Id.ToString()), Times.Once);
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldLogoutOtherSessionsOnly_WhenConfigured()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "old")).ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "old",
                NewPassword = "new",
                LogoutBehavior = LogoutBehavior.LogoutOthersOnly,
                CurrentAccessToken = "access-token"
            };

            var result = await _authService.ChangePasswordAsync(request);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
            _tokenService.Verify(x => x.BlacklistAllUserTokensExceptAsync(user.Id.ToString(), "access-token"), Times.Once);
        }

        [Test]
        public async Task ChangePasswordAsync_ShouldKeepAllSessions_WhenConfigured()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "old")).ReturnsAsync(true);
            _userManager.Setup(x => x.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

            var request = new ChangePasswordRequestDto
            {
                UserId = user.Id,
                CurrentPassword = "old",
                NewPassword = "new",
                LogoutBehavior = LogoutBehavior.KeepAllSessions
            };

            var result = await _authService.ChangePasswordAsync(request);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
            _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(It.IsAny<string>()), Times.Never);
            _tokenService.Verify(x => x.BlacklistAllUserTokensExceptAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        // Tests for RefreshTokenRequestDto validation - missing fields, valid fields, etc.

        #region RefreshTokenRequestDto Validation Tests

        [Test]
        public void RefreshTokenRequestDto_Validation_ShouldFailIfMissingFields()
        {
            var dto = new RefreshTokenRequestDto();
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.That(isValid, Is.False);

            Assert.Multiple(() =>
            {
                Assert.That(results, Has.Count.EqualTo(2));
                Assert.That(results.Any(r => r.ErrorMessage == "Access token is required."));
                Assert.That(results.Any(r => r.ErrorMessage == "Refresh token is required."));
            });
        }

        [Test]
        public void RefreshTokenRequestDto_Validation_ShouldPassWithValidFields()
        {
            var dto = new RefreshTokenRequestDto
            {
                AccessToken = "some-access-token",
                RefreshToken = "some-refresh-token"
            };

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            Assert.Multiple(() =>
            {
                Assert.That(isValid, Is.True);
                Assert.That(results, Is.Empty);
            });
        }

        #endregion

        // Tests for RefreshTokenAsync method - valid token, invalid token, user not found, etc.

        #region RefreshTokenAsync Tests

        [Test]
        public void RefreshTokenAsync_InvalidAccessToken_Throws()
        {
            var dto = new RefreshTokenRequestDto
            {
                AccessToken = "invalid.token",
                RefreshToken = "any"
            };

            Assert.ThrowsAsync<InvalidTokenException>(() => _authService.RefreshTokenAsync(dto));
        }

        [Test]
        public void RefreshTokenAsync_UserNullOrTokenInvalid_Throws()
        {
            var token = GenerateValidExpiredAccessToken("user@email.com");

            _userManager.Setup(m => m.FindByNameAsync("user@email.com"))
                .ReturnsAsync((ApplicationUser?)null);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = token,
                RefreshToken = "wrong"
            };

            Assert.ThrowsAsync<InvalidTokenException>(() => _authService.RefreshTokenAsync(dto));
        }

        [Test]
        public void RefreshTokenAsync_RefreshTokenMismatch_Throws()
        {
            var user = new ApplicationUser
            {
                Email = "user@email.com",
                RefreshToken = "real-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            _userManager.Setup(m => m.FindByNameAsync(user.Email)).ReturnsAsync(user);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = GenerateValidExpiredAccessToken(user.Email),
                RefreshToken = "wrong"
            };

            Assert.ThrowsAsync<InvalidTokenException>(() => _authService.RefreshTokenAsync(dto));
        }

        [Test]
        public void RefreshTokenAsync_RefreshTokenExpired_Throws()
        {
            var user = new ApplicationUser
            {
                Email = "user@email.com",
                RefreshToken = "refresh",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1)
            };

            _userManager.Setup(m => m.FindByNameAsync(user.Email)).ReturnsAsync(user);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = GenerateValidExpiredAccessToken(user.Email),
                RefreshToken = "refresh"
            };

            Assert.ThrowsAsync<InvalidTokenException>(() => _authService.RefreshTokenAsync(dto));
        }

        [Test]
        public void RefreshTokenAsync_UserLockedOut_Throws()
        {
            var user = new ApplicationUser
            {
                Email = "user@email.com",
                RefreshToken = "refresh",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10)
            };

            _userManager.Setup(m => m.FindByNameAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = GenerateValidExpiredAccessToken(user.Email),
                RefreshToken = "refresh"
            };

            Assert.ThrowsAsync<UserAccountLockedException>(() => _authService.RefreshTokenAsync(dto));
        }

        [Test]
        public async Task RefreshTokenAsync_TokenNotExpired_BlacklistCalled()
        {
            var email = "user@email.com";
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                Email = email,
                UserName = email,
                FullName = "User Token",
                RefreshToken = "refresh",
                RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            var token = GenerateValidExpiredAccessToken(email);

            _userManager.Setup(m => m.FindByNameAsync(email)).ReturnsAsync(user);
            _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            _userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _tokenService.Setup(t => t.BlacklistTokenAsync(token, It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = token,
                RefreshToken = user.RefreshToken!
            };

            var result = await _authService.RefreshTokenAsync(dto);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.EqualTo(CustomCode.Success));
                Assert.That(result.Item2.AccessToken, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public async Task RefreshTokenAsync_ValidRequest_ReturnsNewTokens()
        {
            var userId = Guid.NewGuid();
            var email = "refreshuser@example.com";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = email,
                UserName = email,
                FullName = "Refresh User",
                RefreshToken = "old-refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(5)
            };

            var accessToken = GenerateValidExpiredAccessToken(email);

            _userManager.Setup(x => x.FindByNameAsync(email)).ReturnsAsync(user);
            _userManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken!
            };

            var result = await _authService.RefreshTokenAsync(dto);

            Assert.Multiple(() =>
            {
                Assert.That(result.Item1, Is.EqualTo(CustomCode.Success));
                Assert.That(result.Item2.AccessToken, Is.Not.Null.And.Not.Empty);
                Assert.That(result.Item2.RefreshToken, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public async Task RefreshTokenAsync_TokenStillValid_BlacklistsToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var email = "stillvalid@example.com";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = email,
                UserName = email,
                RefreshToken = "refresh-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10)
            };

            var jwtSettings = new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey", "supersecretkey1234567890supersecretkey1234567890" },
                { "JwtSettings:ValidIssuer", "TestIssuer" },
                { "JwtSettings:ValidAudience", "TestAudience" },
                { "JwtSettings:ExpiryInSecond", "3600" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(jwtSettings)
                .Build();

            var key = Encoding.UTF8.GetBytes(jwtSettings["JwtSettings:SecretKey"]!);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: jwtSettings["JwtSettings:ValidIssuer"],
                audience: jwtSettings["JwtSettings:ValidAudience"],
                claims: new[]
                {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                },
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signingCredentials
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            var jwtHandler = new JwtHandler(config, new Mock<ILogger<JwtHandler>>().Object);

            // Setup provider
            var otpLogger = new Mock<ILogger<SixDigitTokenProvider<ApplicationUser>>>();
            var otpOptions = Options.Create(new SixDigitTokenProviderOptions
            {
                TokenLifespan = TimeSpan.FromSeconds(120)
            });
            var otpProvider = new SixDigitTokenProvider<ApplicationUser>(otpOptions, otpLogger.Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SixDigitTokenProvider<ApplicationUser>)))
                .Returns(otpProvider);

            // Mocks
            _userManager.Setup(x => x.FindByNameAsync(email)).ReturnsAsync(user);
            _userManager.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _tokenService.Setup(x => x.BlacklistTokenAsync(token, It.IsAny<DateTime>())).Returns(Task.CompletedTask);

            var authService = new AuthService(
                _userManager.Object,
                _emailSender.Object,
                _logger.Object,
                jwtHandler,
                _tokenService.Object,
                serviceProvider.Object // ✅ Use this mock with OTPProvider setup
            );

            var dto = new RefreshTokenRequestDto
            {
                AccessToken = token,
                RefreshToken = "refresh-token"
            };

            // Act
            var result = await authService.RefreshTokenAsync(dto);

            // Assert
            Assert.That(result.Item1, Is.EqualTo(CustomCode.Success));
            _tokenService.Verify(x => x.BlacklistTokenAsync(token, It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        // Tests for ForgotPasswordAsync method - user not found, valid user, etc.

        #region ForgotPasswordAsync Tests

        [Test]
        public void ForgotPasswordAsync_UserNotFound_Throws()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new ForgotPasswordRequestDto { Email = "ghost@mail.com", ClientUrl = ValidClientUrl };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ForgotPasswordAsync(dto));
        }

        [Test]
        public async Task ForgotPasswordAsync_ValidUser_SendsEmail()
        {
            var user = new ApplicationUser { Email = "user@example.com" };
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

            var dto = new ForgotPasswordRequestDto { Email = user.Email, ClientUrl = ValidClientUrl };
            var result = await _authService.ForgotPasswordAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.ResetPasswordEmailSent));
        }

        [Test]
        public async Task ForgotPasswordAsync_ShouldSendEmail_WithExpectedSubject()
        {
            var user = new ApplicationUser { Email = "forgot@example.com", FullName = "Forgot User" };
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

            _emailSender.Setup(x => x.SendEmailAsync(It.Is<EmailMessage>(m =>
                m.Subject.Contains("Đặt Lại Mật Khẩu")
            ))).Returns(Task.CompletedTask).Verifiable();

            var dto = new ForgotPasswordRequestDto { Email = user.Email, ClientUrl = ValidClientUrl };
            await _authService.ForgotPasswordAsync(dto);

            _emailSender.Verify();
        }

        #endregion

        // Tests for ResetPasswordAsync method - valid token, user not found, new password same as old, etc.

        #region ResetPasswordAsync Tests

        [Test]
        public async Task ResetPasswordAsync_SuccessfulReset_ReturnsSuccess()
        {
            var user = new ApplicationUser { Email = "user@example.com" };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
            _userManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var dto = new ResetPasswordRequestDto
            {
                Email = user.Email,
                Token = "valid-token",
                Password = "new-password"
            };

            var result = await _authService.ResetPasswordAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.PasswordResetSuccessful));
        }

        [Test]
        public void ResetPasswordAsync_InvalidToken_ThrowsAppException()
        {
            var user = new ApplicationUser { Email = "user@example.com" };
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
            _userManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

            var dto = new ResetPasswordRequestDto
            {
                Email = user.Email,
                Token = "invalid-token",
                Password = "new-password"
            };

            Assert.ThrowsAsync<AppException>(() => _authService.ResetPasswordAsync(dto));
        }

        [Test]
        public void ResetPasswordAsync_NewPasswordSameAsOld_ThrowsException()
        {
            var user = new ApplicationUser { Email = "user@example.com" };
            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, "same-password")).ReturnsAsync(true);

            var dto = new ResetPasswordRequestDto
            {
                Email = user.Email,
                Token = "token",
                Password = "same-password"
            };

            Assert.ThrowsAsync<NewPasswordSameAsOldException>(() => _authService.ResetPasswordAsync(dto));
        }

        [Test]
        public void ResetPasswordAsync_UserNotFound_ThrowsUserNotExistsException()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((ApplicationUser?)null);

            var dto = new ResetPasswordRequestDto
            {
                Email = "notfound@example.com",
                Password = "new-password",
                Token = "some-token"
            };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ResetPasswordAsync(dto));
        }

        #endregion

        // Tests for ConfirmEmailAsync method - valid token, user not found, already confirmed, etc.

        #region ConfirmEmailAsync Tests

        [Test]
        public void ConfirmEmailAsync_InvalidToken_Throws()
        {
            var user = new ApplicationUser();
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed());

            var dto = new ConfirmEmailRequestDto { Email = "mail@mail.com", Token = "bad-token" };

            Assert.ThrowsAsync<AppException>(() => _authService.ConfirmEmailAsync(dto));
        }

        [Test]
        public async Task ConfirmEmailAsync_Success_ReturnsSuccess()
        {
            var user = new ApplicationUser();

            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var dto = new ConfirmEmailRequestDto { Email = "mail@example.com", Token = "valid-token" };
            var result = await _authService.ConfirmEmailAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
        }

        [Test]
        public void ConfirmEmailAsync_UserNotFound_ThrowsUserNotExistsException()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                        .ReturnsAsync((ApplicationUser?)null);

            var dto = new ConfirmEmailRequestDto
            {
                Email = "unknown@example.com",
                Token = "token"
            };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ConfirmEmailAsync(dto));
        }

        #endregion

        // Tests for ResendConfirmationEmailAsync method - user not found, already confirmed, etc.

        #region ResendConfirmationEmailAsync Tests

        [Test]
        public void ResendConfirmationEmailAsync_EmailAlreadyConfirmed_Throws()
        {
            var user = new ApplicationUser { EmailConfirmed = true };
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new ResendConfirmationEmailRequestDto { Email = "confirmed@mail.com", ClientUrl = ValidClientUrl };

            Assert.ThrowsAsync<UserAlreadyConfirmedException>(() => _authService.ResendConfirmationEmailAsync(dto));
        }

        [Test]
        public void ResendConfirmationEmailAsync_UserNotFound_Throws()
        {
            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new ResendConfirmationEmailRequestDto { Email = "unknown@mail.com", ClientUrl = ValidClientUrl };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ResendConfirmationEmailAsync(dto));
        }

        [Test]
        public async Task ResendConfirmationEmailAsync_Success_ReturnsCode()
        {
            var user = new ApplicationUser { EmailConfirmed = false };

            _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new ResendConfirmationEmailRequestDto { Email = "notconfirmed@example.com", ClientUrl = ValidClientUrl };
            var result = await _authService.ResendConfirmationEmailAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.ConfirmationEmailSent));
        }

        #endregion

        // Tests for LogoutAsync method - user not found, expired token, valid token.

        #region LogoutAsync Tests

        [Test]
        public async Task LogoutAsync_ExpiredToken_ShouldNotBlacklist()
        {
            var userId = Guid.NewGuid().ToString();
            var user = new ApplicationUser { Id = Guid.Parse(userId) };

            _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

            // Generate expired JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_1234567890123456");

            var now = DateTime.UtcNow;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }),
                NotBefore = now.AddSeconds(-20),              // fix: set NotBefore earlier
                Expires = now.AddSeconds(-10),                // expired
                Issuer = "issuer",
                Audience = "audience",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var expiredToken = tokenHandler.WriteToken(token);

            await _authService.LogoutAsync(userId, expiredToken);

            _tokenService.Verify(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Test]
        public async Task LogoutAsync_TokenValid_ShouldBlacklist()
        {
            var userId = Guid.NewGuid().ToString();
            var user = new ApplicationUser { Id = Guid.Parse(userId) };

            _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                        .ReturnsAsync(IdentityResult.Success);

            var token = GenerateJwtAccessToken(userId, DateTime.UtcNow.AddMinutes(10));

            _tokenService.Setup(x => x.BlacklistTokenAsync(token, It.IsAny<DateTime>()))
                         .Returns(Task.CompletedTask);

            await _authService.LogoutAsync(userId, token);

            _tokenService.Verify(x => x.BlacklistTokenAsync(
                token,
                It.Is<DateTime>(dt => dt > DateTime.UtcNow)), Times.Once);
        }

        [Test]
        public async Task LogoutAsync_UserNotFound_ShouldNotThrow()
        {
            var userId = Guid.NewGuid().ToString();
            var token = GenerateJwtAccessToken(userId, DateTime.UtcNow.AddMinutes(5));

            _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            await _authService.LogoutAsync(userId, token);

            _userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _tokenService.Verify(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        // Tests for RequestEnable2FaOtpAsync method - user not found, already enabled, wrong password, etc.

        #region RequestEnable2FaOtpAsync Tests

        [Test]
        public async Task RequestEnable2FaOtpAsync_Success_ReturnsOtpSent()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "correct" };
            var result = await _authService.RequestEnable2FaOtpAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.OtpSentSuccessfully));
        }

        [Test]
        public void RequestEnable2FaOtpAsync_UserNotFound_ThrowsException()
        {
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "password" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.RequestEnable2FaOtpAsync(dto));
        }

        [Test]
        public void RequestEnable2FaOtpAsync_AlreadyEnabled_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "password" };

            Assert.ThrowsAsync<TwoFactorIsAlreadyEnabledException>(() => _authService.RequestEnable2FaOtpAsync(dto));
        }

        [Test]
        public void RequestEnable2FaOtpAsync_WrongPassword_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "wrong" };

            Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.RequestEnable2FaOtpAsync(dto));
        }

        #endregion

        // Tests for ConfirmEnable2FaOtpAsync method - valid OTP, user not found, already enabled, etc.

        #region ConfirmEnable2FaOtpAsync Tests

        [Test]
        public async Task ConfirmEnable2FaOtpAsync_Valid_ReturnsSuccess()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };
            var result = await _authService.ConfirmEnable2FaOtpAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
        }

        [Test]
        public void ConfirmEnable2FaOtpAsync_UpdateFails_ThrowsAppException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Failed to enable 2FA"
            }));

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

            var ex = Assert.ThrowsAsync<AppException>(() => _authService.ConfirmEnable2FaOtpAsync(dto));

            Assert.Multiple(() =>
            {
                Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
                Assert.That(ex.Errors, Has.One.EqualTo("Failed to enable 2FA"));
            });
        }

        [Test]
        public void ConfirmEnable2FaOtpAsync_UserNotFound_ThrowsException()
        {
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "code" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ConfirmEnable2FaOtpAsync(dto));
        }

        [Test]
        public void ConfirmEnable2FaOtpAsync_AlreadyEnabled_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

            Assert.ThrowsAsync<TwoFactorIsAlreadyEnabledException>(() => _authService.ConfirmEnable2FaOtpAsync(dto));
        }

        #endregion

        // Tests for RequestDisable2FaOtpAsync method - user not found, already disabled, invalid password, etc.

        #region RequestDisable2FaOtpAsync Tests

        [Test]
        public void RequestDisable2FaOtpAsync_UserNotFound_ThrowsException()
        {
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                        .ReturnsAsync((ApplicationUser?)null);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "password" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.RequestDisable2FaOtpAsync(dto));
        }

        [Test]
        public void RequestDisable2FaOtpAsync_TwoFactorAlreadyDisabled_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "password" };

            Assert.ThrowsAsync<TwoFactorIsAlreadyDisabledException>(() => _authService.RequestDisable2FaOtpAsync(dto));
        }

        [Test]
        public void RequestDisable2FaOtpAsync_InvalidPassword_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "wrong" };

            Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.RequestDisable2FaOtpAsync(dto));
        }

        [Test]
        public async Task RequestDisable2FaOtpAsync_ValidRequest_ReturnsSuccess()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.GenerateTwoFactorTokenAsync(user, It.IsAny<string>()))
                        .ReturnsAsync("token");

            _emailSender.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()))
                        .Returns(Task.CompletedTask);

            var dto = new Request2FaDto { UserId = Guid.NewGuid(), CurrentPassword = "correct" };

            var result = await _authService.RequestDisable2FaOtpAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.OtpSentSuccessfully));
        }

        #endregion

        // Tests for ConfirmDisable2FaOtpAsync method - valid OTP, user not found, already disabled, etc.

        #region ConfirmDisable2FaOtpAsync Tests

        [Test]
        public void ConfirmDisable2FaOtpAsync_UpdateFails_ThrowsAppException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Description = "Failed to disable 2FA"
            }));

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

            var ex = Assert.ThrowsAsync<AppException>(() => _authService.ConfirmDisable2FaOtpAsync(dto));

            Assert.Multiple(() =>
            {
                Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
                Assert.That(ex.Errors, Contains.Item("Failed to disable 2FA"));
            });
        }

        [Test]
        public void ConfirmDisable2FaOtpAsync_UserNotFound_Throws()
        {
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "code" };

            Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ConfirmDisable2FaOtpAsync(dto));
        }

        [Test]
        public async Task ConfirmDisable2FaOtpAsync_Valid_ReturnsSuccess()
        {
            var user = new ApplicationUser { TwoFactorEnabled = true };

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };
            var result = await _authService.ConfirmDisable2FaOtpAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.Success));
        }

        [Test]
        public void ConfirmDisable2FaOtpAsync_AlreadyDisabled_ThrowsException()
        {
            var user = new ApplicationUser { TwoFactorEnabled = false };
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

            var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

            Assert.ThrowsAsync<TwoFactorIsAlreadyDisabledException>(() => _authService.ConfirmDisable2FaOtpAsync(dto));
        }

        #endregion

        // Tests for InvalidateAllUserTokensAsync method - user not found, valid user, etc.

        #region InvalidateAllUserTokensAsync Tests

        [Test]
        public async Task InvalidateAllUserTokensAsync_ValidUser_BlacklistsAll()
        {
            var userId = Guid.NewGuid().ToString();
            var user = new ApplicationUser { Id = Guid.Parse(userId) };

            _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _authService.InvalidateAllUserTokensAsync(userId);

            _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
            _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(userId), Times.Once);
        }

        [Test]
        public async Task InvalidateAllUserTokensAsync_UserNotFound_StillBlacklists()
        {
            var userId = Guid.NewGuid().ToString();
            _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            await _authService.InvalidateAllUserTokensAsync(userId);

            _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(userId), Times.Once);
        }

        #endregion

        // Send2FaOtpEmailAsync method tests - valid user, email sending, etc.

        #region Send2FaOtpEmailAsync Tests

        [Test]
        public async Task Send2FaOtpEmailAsync_ShouldSendWithCorrectSubject()
        {
            var user = new ApplicationUser { Email = "user@example.com", FullName = "User" };
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            var method = typeof(AuthService).GetMethod("Send2FaOtpEmailAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

            _emailSender.Setup(x => x.SendEmailAsync(It.Is<EmailMessage>(m =>
                m.Subject.Contains("Bật xác thực 2 yếu tố")
            ))).Returns(Task.CompletedTask).Verifiable();

            await (Task)method.Invoke(_authService, [user, "Bật xác thực 2 yếu tố - EDUVA"])!;

            _emailSender.Verify();
        }

        #endregion

        // Tests for Confirm2FaChangeAsync method - valid OTP, user not found, invalid OTP, etc.

        #region Confirm2FaChangeAsync Tests

        [Test]
        public async Task Confirm2FaChangeAsync_InvalidOtp_ThrowsException()
        {
            var user = new ApplicationUser();

            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(false);

            var method = typeof(AuthService).GetMethod("Confirm2FaChangeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var resultTask = (Task)method.Invoke(_authService, new object[] { user, "123456", true })!;

            try
            {
                await resultTask;
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (OtpInvalidOrExpireException ex)
            {
                Assert.That(ex.Message, Is.EqualTo("OTP code is invalid or has expired."));
            }
        }

        [Test]
        public async Task Confirm2FaChangeAsync_ShouldClearOtpClaims_WhenValid()
        {
            var user = new ApplicationUser();
            _userManager.Setup(x => x.VerifyTwoFactorTokenAsync(user, "OTP", "123456")).ReturnsAsync(true);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
        {
            new Claim("LastOtpSentTime", DateTime.UtcNow.ToString("o")),
            new Claim("LastOtpValue", "123456")
        });

            var method = typeof(AuthService).GetMethod("Confirm2FaChangeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var result = await (Task<CustomCode>)method.Invoke(_authService, [user, "123456", true])!;

            Assert.That(result, Is.EqualTo(CustomCode.Success));
            _userManager.Verify(x => x.RemoveClaimAsync(user, It.IsAny<Claim>()), Times.Exactly(2));
        }


        #endregion

        // Tests for SendOtpEmailMessage method - valid user, template file not found, etc.

        #region SendConfirmEmailMessage_TemplateFileNotFound_ThrowsFileNotFoundException

        [Test]
        public void SendConfirmEmailMessage_TemplateFileNotFound_ThrowsFileNotFoundException()
        {
            var user = new ApplicationUser { Email = "test@example.com" };

            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", "verify-email.html");
            if (File.Exists(templatePath)) File.Delete(templatePath);

            _userManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token");

            var ex = Assert.ThrowsAsync<FileNotFoundException>(() =>
            {
                var method = typeof(AuthService).GetMethod("SendConfirmEmailMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                return (Task)method!.Invoke(_authService, new object[] { ValidClientUrl, user })!;
            });

            Assert.That(ex!.Message, Contains.Substring("Template file not found"));
        }

        #endregion

        // Tests for SendOtpEmailMessage method - user not found, valid user, template file not found.

        #region SendOtpEmailMessage_TemplateFileNotFound_ThrowsFileNotFoundException

        [Test]
        public void SendOtpEmailMessage_TemplateFileNotFound_ThrowsFileNotFoundException()
        {
            var user = new ApplicationUser { Email = "otp@example.com" };

            var basePath = AppContext.BaseDirectory;
            var templatePath = Path.Combine(basePath, "email-templates", "otp-verification.html");
            if (File.Exists(templatePath)) File.Delete(templatePath);

            var method = typeof(AuthService).GetMethod("SendOtpEmailMessage", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method!.Invoke(_authService, new object[] { user, "123456" })!;

            Assert.ThrowsAsync<FileNotFoundException>(() => task);
        }

        #endregion

        // Tests for ResendOtpAsync method - user not found, valid user, already sent, etc.

        #region ResendOtpAsync Tests

        [Test]
        public async Task ResendOtpAsync_NoLastOtpSentClaim_AllowsResend()
        {
            var user = new ApplicationUser { Email = "test@example.com" };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _emailSender.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>())).Returns(Task.CompletedTask);

            var dto = new ResendOtpRequestDto
            {
                Email = user.Email,
                Purpose = OtpPurpose.Login
            };

            var result = await _authService.ResendOtpAsync(dto);

            Assert.That(result, Is.EqualTo(CustomCode.OtpSentSuccessfully));
            _emailSender.Verify(x => x.SendEmailAsync(It.IsAny<EmailMessage>()), Times.Once);
        }

        [Test]
        public void ResendOtpAsync_ThrottleTooSoon_ThrowsAppException()
        {
            var user = new ApplicationUser { Email = "test@example.com" };
            var now = DateTime.UtcNow;

            _userManager.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
        {
            new Claim("LastOtpSentTime", now.ToString("o"))
        });

            var dto = new ResendOtpRequestDto
            {
                Email = user.Email,
                Purpose = OtpPurpose.Login
            };

            Assert.ThrowsAsync<AppException>(() => _authService.ResendOtpAsync(dto));
        }

        #endregion

        // ForceClearOtpClaimsAsync method - clears OTP claims for a user.

        #region ForceClearOtpClaimsAsync Tests

        [Test]
        public async Task ForceClearOtpClaimsAsync_NoOtpClaims_ShouldNotThrow()
        {
            var user = new ApplicationUser();

            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

            var provider = new SixDigitTokenProvider<ApplicationUser>(
                Options.Create(new SixDigitTokenProviderOptions { TokenLifespan = TimeSpan.FromSeconds(60) }),
                new Mock<ILogger<SixDigitTokenProvider<ApplicationUser>>>().Object);

            await provider.ForceClearOtpClaimsAsync(_userManager.Object, user);

            _userManager.Verify(x => x.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()), Times.Never);
        }

        #endregion

        // CheckOtpThrottleAsync method - checks if the user can send an OTP based on throttle settings.

        #region CheckOtpThrottleAsync Tests

        [Test]
        public async Task CheckOtpThrottleAsync_InvalidFormat_ShouldNotThrow()
        {
            var user = new ApplicationUser();

            _userManager.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
        {
            new Claim("LastOtpSentTime", "invalid-date-format")
        });

            var logger = new Mock<ILogger<SixDigitTokenProvider<ApplicationUser>>>();
            var provider = new SixDigitTokenProvider<ApplicationUser>(
                Options.Create(new SixDigitTokenProviderOptions { TokenLifespan = TimeSpan.FromSeconds(120) }),
                logger.Object);

            await provider.CheckOtpThrottleAsync(_userManager.Object, user);

            Assert.Pass("No exception thrown");
        }

        #endregion


        #region Helper Methods

        // Tests for GenerateValidExpiredAccessToken method - generates a valid expired JWT token.
        private static string GenerateValidExpiredAccessToken(string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_1234567890123456");

            var now = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }),
                NotBefore = now.AddMinutes(-10),
                Expires = now.AddMinutes(-5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = "issuer",
                Audience = "audience"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Generates a JWT access token for testing purposes.
        private static string GenerateJwtAccessToken(string userId, DateTime expiry)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_1234567890123456");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }),
                Expires = expiry,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = "issuer",
                Audience = "audience"
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        // Mocks the UserManager for ApplicationUser.
        private static Mock<UserManager<T>> MockUserManager<T>() where T : class
        {
            var store = new Mock<IUserStore<T>>();
            return new Mock<UserManager<T>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        // Create Email Template
        private static void CreateEmailTemplate(string fileName, string content)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "email-templates");
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, fileName);
            File.WriteAllText(filePath, content);
        }

        #endregion

    }
}