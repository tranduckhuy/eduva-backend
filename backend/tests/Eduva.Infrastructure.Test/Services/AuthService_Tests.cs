using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Auth.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Identity;
using Eduva.Infrastructure.Identity.Interfaces;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace Eduva.Application.Test.Services;

[TestFixture]
public class AuthService_Tests
{
    private Mock<UserManager<ApplicationUser>> _userManager = default!;
    private Mock<IEmailSender> _emailSender = default!;
    private Mock<ILogger<AuthService>> _logger = default!;
    private Mock<ITokenBlackListService> _tokenService = default!;
    private AuthService _authService = default!;
    private const string ValidClientUrl = "https://localhost:9001/api/auth/confirm-email";

    [SetUp]
    public void Setup()
    {
        _userManager = MockUserManager<ApplicationUser>();
        _emailSender = new Mock<IEmailSender>();
        _logger = new Mock<ILogger<AuthService>>();
        _tokenService = new Mock<ITokenBlackListService>();

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

        _authService = new AuthService(
            _userManager.Object,
            _emailSender.Object,
            _logger.Object,
            jwtHandler,
            _tokenService.Object);
    }

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
    public void LoginAsync_InvalidPassword_ThrowsException()
    {
        var user = new ApplicationUser { Email = "user@example.com", EmailConfirmed = true };
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

        var dto = new LoginRequestDto { Email = user.Email, Password = "wrong" };

        Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(dto));
    }

    [Test]
    public void LoginAsync_UserLockedOut_ThrowsException()
    {
        var user = new ApplicationUser { Email = "user@example.com", EmailConfirmed = true };
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var dto = new LoginRequestDto { Email = user.Email, Password = "password" };

        Assert.ThrowsAsync<UserAccountLockedException>(() => _authService.LoginAsync(dto));
    }

    [Test]
    public async Task LoginAsync_2FAEnabled_ReturnsOtpRequired()
    {
        var user = new ApplicationUser { Email = "user@example.com", EmailConfirmed = true, FullName = "User" };
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        _userManager.Setup(m => m.GenerateTwoFactorTokenAsync(user, It.IsAny<string>())).ReturnsAsync("otp-token");

        var dto = new LoginRequestDto { Email = user.Email, Password = "password" };
        var result = await _authService.LoginAsync(dto);

        Assert.Multiple(() =>
        {
            Assert.That(result.Item1, Is.EqualTo(CustomCode.RequiresOtpVerification));
            Assert.That(result.Item2.Requires2FA, Is.True);
            Assert.That(result.Item2.Email, Is.EqualTo(user.Email));
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

    private static Mock<UserManager<T>> MockUserManager<T>() where T : class
    {
        var store = new Mock<IUserStore<T>>();
        return new Mock<UserManager<T>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Test]
    public async Task ChangePasswordAsync_KeepAllSessions_ShouldNotInvalidateAnything()
    {
        var user = new ApplicationUser();
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        _userManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ChangePasswordRequestDto
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            LogoutBehavior = LogoutBehavior.KeepAllSessions
        };

        var result = await _authService.ChangePasswordAsync(dto);

        Assert.That(result, Is.EqualTo(CustomCode.Success));
        _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(It.IsAny<string>()), Times.Never);
        _tokenService.Verify(x => x.BlacklistAllUserTokensExceptAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ChangePasswordAsync_LogoutOthersOnly_ShouldKeepCurrentToken()
    {
        var user = new ApplicationUser();
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        _userManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ChangePasswordRequestDto
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            LogoutBehavior = LogoutBehavior.LogoutOthersOnly,
            CurrentAccessToken = "token123"
        };

        var result = await _authService.ChangePasswordAsync(dto);

        Assert.That(result, Is.EqualTo(CustomCode.Success));
        _tokenService.Verify(x => x.BlacklistAllUserTokensExceptAsync(dto.UserId.ToString(), dto.CurrentAccessToken), Times.Once);
    }

    [Test]
    public async Task ChangePasswordAsync_LogoutAllIncludingCurrent_ShouldInvalidateAll()
    {
        var user = new ApplicationUser();
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        _userManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ChangePasswordRequestDto
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            LogoutBehavior = LogoutBehavior.LogoutAllIncludingCurrent
        };

        var result = await _authService.ChangePasswordAsync(dto);

        Assert.That(result, Is.EqualTo(CustomCode.Success));
        _tokenService.Verify(x => x.BlacklistAllUserTokensAsync(dto.UserId.ToString()), Times.Once);
    }

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

        Assert.That(isValid, Is.True);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ForgotPasswordAsync_UserNotFound_Throws()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var dto = new ForgotPasswordRequestDto { Email = "ghost@mail.com", ClientUrl = ValidClientUrl };

        Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ForgotPasswordAsync(dto));
    }

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
    public void ResendConfirmationEmailAsync_EmailAlreadyConfirmed_Throws()
    {
        var user = new ApplicationUser { EmailConfirmed = true };
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var dto = new ResendConfirmationEmailRequestDto { Email = "confirmed@mail.com", ClientUrl = ValidClientUrl };

        Assert.ThrowsAsync<UserAlreadyConfirmedException>(() => _authService.ResendConfirmationEmailAsync(dto));
    }

    [Test]
    public async Task LogoutAsync_ExpiredToken_ShouldNotBlacklist()
    {
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = Guid.Parse(userId) };

        _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        // Generate expired JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = System.Text.Encoding.UTF8.GetBytes("super_secret_key_1234567890123456");

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
    public async Task ResendConfirmationEmailAsync_Success_ReturnsCode()
    {
        var user = new ApplicationUser { EmailConfirmed = false };

        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);

        var dto = new ResendConfirmationEmailRequestDto { Email = "notconfirmed@example.com", ClientUrl = ValidClientUrl };
        var result = await _authService.ResendConfirmationEmailAsync(dto);

        Assert.That(result, Is.EqualTo(CustomCode.ConfirmationEmailSent));
    }

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

    [Test]
    public void ConfirmEnable2FaOtpAsync_AlreadyEnabled_ThrowsException()
    {
        var user = new ApplicationUser { TwoFactorEnabled = true };
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

        Assert.ThrowsAsync<TwoFactorIsAlreadyEnabledException>(() => _authService.ConfirmEnable2FaOtpAsync(dto));
    }

    [Test]
    public void ConfirmDisable2FaOtpAsync_AlreadyDisabled_ThrowsException()
    {
        var user = new ApplicationUser { TwoFactorEnabled = false };
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);

        var dto = new Confirm2FaDto { UserId = Guid.NewGuid(), OtpCode = "123456" };

        Assert.ThrowsAsync<TwoFactorIsAlreadyDisabledException>(() => _authService.ConfirmDisable2FaOtpAsync(dto));
    }

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
    public void ResendConfirmationEmailAsync_UserNotFound_Throws()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var dto = new ResendConfirmationEmailRequestDto { Email = "unknown@mail.com", ClientUrl = ValidClientUrl };

        Assert.ThrowsAsync<UserNotExistsException>(() => _authService.ResendConfirmationEmailAsync(dto));
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
    public void RefreshTokenAsync_InvalidAccessToken_ThrowsException()
    {
        var dto = new RefreshTokenRequestDto
        {
            AccessToken = "invalid.token",
            RefreshToken = "refresh"
        };

        Assert.ThrowsAsync<InvalidTokenException>(() => _authService.RefreshTokenAsync(dto));
    }
}