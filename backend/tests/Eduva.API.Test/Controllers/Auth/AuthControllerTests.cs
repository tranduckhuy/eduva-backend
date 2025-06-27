using Eduva.API.Controllers.Auth;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Auth.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Auth
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<IAuthService> _authServiceMock;
        private Mock<ILogger<AuthController>> _loggerMock;
        private AuthController _controller;
        private const string ValidClientUrl = "https://localhost:9001/api/auth/confirm-email";

        #region AuthController Setup

        [SetUp]
        public void Setup()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        #endregion

        // Tests for Register method - 200, 400, 500 responses

        #region Register Tests

        [Test]
        public async Task Register_ShouldReturn200_WhenSuccessful()
        {
            var request = new RegisterRequestDto
            {
                Email = "sang@gmail.com",
                Password = "Sangtran1309!",
                ConfirmPassword = "Sangtran1309!",
                FullName = "Tran Ngoc Sang",
                PhoneNumber = "0935323123",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(CustomCode.ConfirmationEmailSent);

            var result = await _controller.Register(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task Register_ShouldReturn400_WhenEmailAlreadyExists()
        {
            var request = new RegisterRequestDto
            {
                Email = "duplicate@gmail.com",
                Password = "Sangtran1309!",
                ConfirmPassword = "Sangtran1309!",
                FullName = "Dup User",
                PhoneNumber = "0935323123",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new AppException(CustomCode.EmailAlreadyExists, new List<string> { "Email already exists" }));

            var result = await _controller.Register(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task Register_ShouldReturn500_WhenUnhandledExceptionOccurs()
        {
            var request = new RegisterRequestDto
            {
                Email = "error@gmail.com",
                Password = "Sangtran1309!",
                ConfirmPassword = "Sangtran1309!",
                FullName = "System Error",
                PhoneNumber = "0935323123",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.RegisterAsync(request))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.Register(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task Register_ShouldReturn400_WhenAllRequiredFieldsMissing()
        {
            var request = new RegisterRequestDto();

            _controller.ModelState.AddModelError("FullName", "Full name is required");
            _controller.ModelState.AddModelError("Email", "Email is required");
            _controller.ModelState.AddModelError("PhoneNumber", "Phone number is required");
            _controller.ModelState.AddModelError("Password", "Password is required");
            _controller.ModelState.AddModelError("ConfirmPassword", "Confirm Password is required");
            _controller.ModelState.AddModelError("ClientUrl", "Client URL is required");

            var result = await _controller.Register(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        #endregion

        // Tests for Login method - 200, 403, 400, 401 responses

        #region Login Tests

        [Test]
        public async Task Login_ShouldReturn200_WithCorrectAuthResult()
        {
            var request = new LoginRequestDto
            {
                Email = "user@example.com",
                Password = "StrongPass123!"
            };

            var expectedResult = new AuthResultDto
            {
                AccessToken = "access-token-123",
                RefreshToken = "refresh-token-456",
                ExpiresIn = 3600,
                Requires2FA = false,
                Email = request.Email
            };

            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ReturnsAsync((CustomCode.Success, expectedResult));

            var result = await _controller.Login(request);
            var objectResult = result as ObjectResult;

            Assert.Multiple(() =>
            {
                Assert.That(objectResult, Is.Not.Null);
                Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

                var apiResponse = objectResult.Value as ApiResponse<object>;
                Assert.That(apiResponse, Is.Not.Null);

                var authResult = apiResponse!.Data as AuthResultDto;
                Assert.That(authResult, Is.Not.Null);
                Assert.That(authResult!.AccessToken, Is.EqualTo("access-token-123"));
                Assert.That(authResult.RefreshToken, Is.EqualTo("refresh-token-456"));
                Assert.That(authResult.ExpiresIn, Is.EqualTo(3600));
                Assert.That(authResult.Requires2FA, Is.False);
                Assert.That(authResult.Email, Is.EqualTo("user@example.com"));
            });
        }

        [Test]
        public async Task Login_ShouldReturn200_WhenRequiresOtp()
        {
            var request = new LoginRequestDto
            {
                Email = "2fa@gmail.com",
                Password = "Sangtran1309!"
            };

            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ReturnsAsync((CustomCode.RequiresOtpVerification, new AuthResultDto { Requires2FA = true, Email = request.Email }));

            var result = await _controller.Login(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task Login_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new LoginRequestDto();

            _controller.ModelState.AddModelError("Email", "Email is required");

            var result = await _controller.Login(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task Login_ShouldReturn401_WhenInvalidCredentials()
        {
            var request = new LoginRequestDto
            {
                Email = "wrong@example.com",
                Password = "wrongpass"
            };

            _authServiceMock.Setup(s => s.LoginAsync(request))
                .ThrowsAsync(new AppException(CustomCode.InvalidCredentials));

            var result = await _controller.Login(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        // Tests for VerifyOtpLogin method - 200, 401 responses

        #region VerifyOtpLogin Tests

        [Test]
        public async Task VerifyOtp_ShouldReturn200_WhenCorrectCode()
        {
            var request = new VerifyOtpRequestDto
            {
                Email = "sang@gmail.com",
                OtpCode = "123456"
            };

            _authServiceMock.Setup(s => s.VerifyLoginOtpAsync(request))
                .ReturnsAsync((CustomCode.Success, new AuthResultDto()));

            var result = await _controller.VerifyOtpLogin(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task VerifyOtp_ShouldReturn401_WhenInvalidCode()
        {
            var request = new VerifyOtpRequestDto
            {
                Email = "sang@gmail.com",
                OtpCode = "wrong"
            };

            _authServiceMock.Setup(s => s.VerifyLoginOtpAsync(request))
                .ThrowsAsync(new AppException(CustomCode.OtpInvalidOrExpired, new List<string> { "Invalid OTP" }));

            var result = await _controller.VerifyOtpLogin(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        // Tests for ForgotPassword method - 200, 404 responses

        #region ForgotPassword Tests

        [Test]
        public async Task ForgotPassword_ShouldReturn200_WhenSuccessful()
        {
            var request = new ForgotPasswordRequestDto
            {
                Email = "sang@gmail.com",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.ForgotPasswordAsync(request))
                .ReturnsAsync(CustomCode.ResetPasswordEmailSent);

            var result = await _controller.ForgotPassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ForgotPassword_ShouldReturn404_WhenUserNotExists()
        {
            var request = new ForgotPasswordRequestDto
            {
                Email = "notfound@gmail.com",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.ForgotPasswordAsync(request))
                .ThrowsAsync(new AppException(CustomCode.UserNotExists, new List<string> { "User not found" }));

            var result = await _controller.ForgotPassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        #endregion

        // Tests for ResetPassword method - 200, 400, 500 responses

        #region ResetPassword Tests

        [Test]
        public async Task ResetPassword_ShouldReturn200_WhenSuccessful()
        {
            var request = new ResetPasswordRequestDto
            {
                Email = "user@example.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!",
                Token = "valid-token"
            };

            _authServiceMock.Setup(s => s.ResetPasswordAsync(request))
                .ReturnsAsync(CustomCode.PasswordResetSuccessful);

            var result = await _controller.ResetPassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ResetPassword_ShouldReturn400_WhenAppExceptionOccurs()
        {
            var request = new ResetPasswordRequestDto
            {
                Email = "user@example.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!",
                Token = "invalid-token"
            };

            _authServiceMock.Setup(s => s.ResetPasswordAsync(request))
                .ThrowsAsync(new AppException(CustomCode.ConfirmEmailTokenInvalidOrExpired));

            var result = await _controller.ResetPassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task ResetPassword_ShouldReturn500_WhenUnhandledExceptionOccurs()
        {
            var request = new ResetPasswordRequestDto
            {
                Email = "user@example.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!",
                Token = "valid-token"
            };

            _authServiceMock.Setup(s => s.ResetPasswordAsync(request))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.ResetPassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for ConfirmEmail method - 200, 400 responses

        #region ConfirmEmail Tests

        [Test]
        public async Task ConfirmEmail_ShouldReturn200_WhenSuccessful()
        {
            var request = new ConfirmEmailRequestDto
            {
                Email = "user@example.com",
                Token = "valid-token"
            };

            _authServiceMock.Setup(s => s.ConfirmEmailAsync(request))
                .ReturnsAsync(CustomCode.Success);

            var result = await _controller.ConfirmEmail(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ConfirmEmail_ShouldReturn400_WhenTokenInvalid()
        {
            var request = new ConfirmEmailRequestDto { Email = "user@example.com", Token = "invalid-token" };

            _authServiceMock.Setup(s => s.ConfirmEmailAsync(request))
                .ThrowsAsync(new AppException(CustomCode.ConfirmEmailTokenInvalidOrExpired));

            var result = await _controller.ConfirmEmail(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        #endregion

        // Tests for ResendConfirmationEmail method - 200, 400 responses

        #region ResendConfirmationEmail Tests

        [Test]
        public async Task ResendConfirmationEmail_ShouldReturn200_WhenSuccessful()
        {
            var request = new ResendConfirmationEmailRequestDto
            {
                Email = "user@example.com",
                ClientUrl = ValidClientUrl
            };

            _authServiceMock.Setup(s => s.ResendConfirmationEmailAsync(request))
                .ReturnsAsync(CustomCode.ConfirmationEmailSent);

            var result = await _controller.ResendConfirmationEmail(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ResendConfirmationEmail_ShouldReturn400_WhenUserAlreadyConfirmed()
        {
            var request = new ResendConfirmationEmailRequestDto { Email = "confirmed@example.com", ClientUrl = ValidClientUrl };

            _authServiceMock.Setup(s => s.ResendConfirmationEmailAsync(request))
                .ThrowsAsync(new AppException(CustomCode.UserAlreadyConfirmed));

            var result = await _controller.ResendConfirmationEmail(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task ResendConfirmationEmail_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new ResendConfirmationEmailRequestDto();

            _controller.ModelState.AddModelError("Email", "Email is required");

            var result = await _controller.ResendConfirmationEmail(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        #endregion

        // Tests for RefreshToken method - 200, 400, 401, 500 responses

        #region RefreshToken Tests

        [Test]
        public async Task RefreshToken_ShouldReturn200_WhenSuccessful()
        {
            var request = new RefreshTokenRequestDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _authServiceMock.Setup(s => s.RefreshTokenAsync(request))
                .ReturnsAsync((CustomCode.Success, new AuthResultDto()));

            var result = await _controller.RefreshToken(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task RefreshToken_ShouldReturn500_WhenExceptionThrown()
        {
            var request = new RefreshTokenRequestDto
            {
                AccessToken = "token",
                RefreshToken = "refresh"
            };

            _authServiceMock.Setup(s => s.RefreshTokenAsync(request))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.RefreshToken(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task RefreshToken_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new RefreshTokenRequestDto();
            _controller.ModelState.AddModelError("AccessToken", "AccessToken is required");

            var result = await _controller.RefreshToken(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task RefreshToken_ShouldReturn401_WhenUnauthorized()
        {
            var request = new RefreshTokenRequestDto
            {
                AccessToken = "invalid",
                RefreshToken = "invalid"
            };

            _authServiceMock.Setup(s => s.RefreshTokenAsync(request))
                .ThrowsAsync(new AppException(CustomCode.Unauthorized));

            var result = await _controller.RefreshToken(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        // Tests for Logout method - 200, 401 responses

        #region Logout Tests

        [Test]
        public async Task Logout_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid().ToString();
            var token = "Bearer some.token.value";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }));
            _controller.ControllerContext.HttpContext.Request.Headers.TryAdd("Authorization", token);

            _authServiceMock.Setup(s => s.LogoutAsync(userId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Logout();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task Logout_ShouldReturn401_WhenUserIdMissing()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.Logout();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        // Tests for InvalidateAllSessions method - 200, 401 responses

        #region InvalidateAllSessions Tests

        [Test]
        public async Task InvalidateAllSessions_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid().ToString();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }));

            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .Returns(Task.CompletedTask);

            var result = await _controller.InvalidateAllSessions();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task InvalidateAllSessions_ShouldReturn401_WhenUserIdMissing()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.InvalidateAllSessions();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task InvalidateAllSessions_ShouldReturn403_WhenForbidden()
        {
            var userId = Guid.NewGuid().ToString();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }));

            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .ThrowsAsync(new AppException(CustomCode.Forbidden));

            var result = await _controller.InvalidateAllSessions();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }

        [Test]
        public async Task InvalidateAllSessions_ShouldReturn500_WhenUnhandledExceptionOccurs()
        {
            var userId = Guid.NewGuid().ToString();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }));

            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.InvalidateAllSessions();
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for InvalidateUserTokens method - 200, 500, 401, 403 responses

        #region InvalidateUserTokens Tests

        [Test]
        public async Task InvalidateUserTokens_ShouldReturn200_WhenCalledByAdmin()
        {
            var userId = Guid.NewGuid().ToString();

            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .Returns(Task.CompletedTask);

            var result = await _controller.InvalidateUserTokens(userId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task InvalidateUserTokens_ShouldReturn500_WhenUnhandledExceptionOccurs()
        {
            var userId = Guid.NewGuid().ToString();

            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.InvalidateUserTokens(userId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task InvalidateUserTokens_ShouldReturn401_WhenUserIdInvalid()
        {
            string userId = "";

            var result = await _controller.InvalidateUserTokens(userId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task InvalidateUserTokens_ShouldReturn403_WhenNotAuthorized()
        {
            var userId = Guid.NewGuid().ToString();
            _authServiceMock.Setup(s => s.InvalidateAllUserTokensAsync(userId))
                .ThrowsAsync(new AppException(CustomCode.Forbidden));

            var result = await _controller.InvalidateUserTokens(userId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }

        #endregion

        // Tests for ChangePassword method - 200, 401 responses

        #region ChangePassword Tests

        [Test]
        public async Task ChangePassword_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var token = "Bearer abc.def.ghi";

            var request = new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPass@123",
                NewPassword = "NewPass@123",
                LogoutBehavior = LogoutBehavior.LogoutAllIncludingCurrent
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));
            _controller.ControllerContext.HttpContext.Request.Headers.TryAdd("Authorization", token);

            _authServiceMock.Setup(s => s.ChangePasswordAsync(It.Is<ChangePasswordRequestDto>(r => r.UserId == userId)))
                .ReturnsAsync(CustomCode.Success);

            var result = await _controller.ChangePassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ChangePassword_ShouldReturn401_WhenUserIdMissing()
        {
            var request = new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPass@123",
                NewPassword = "NewPass@123",
                LogoutBehavior = LogoutBehavior.KeepAllSessions
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.ChangePassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task ChangePassword_ShouldReturn401_WhenAccessTokenMissing()
        {
            var userId = Guid.NewGuid();
            var request = new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPass@123",
                NewPassword = "NewPass@123",
                LogoutBehavior = LogoutBehavior.KeepAllSessions
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            var result = await _controller.ChangePassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task ChangePassword_ShouldSetAccessTokenFromHeader()
        {
            var userId = Guid.NewGuid();
            var token = "Bearer myaccesstoken";

            var request = new ChangePasswordRequestDto
            {
                CurrentPassword = "OldPassword@1",
                NewPassword = "NewPassword@1",
                LogoutBehavior = LogoutBehavior.LogoutOthersOnly
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));
            _controller.ControllerContext.HttpContext.Request.Headers.TryAdd("Authorization", token);

            _authServiceMock.Setup(s => s.ChangePasswordAsync(It.Is<ChangePasswordRequestDto>(r =>
                r.CurrentAccessToken == "myaccesstoken" && r.UserId == userId)))
                .ReturnsAsync(CustomCode.Success);

            var result = await _controller.ChangePassword(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        #endregion

        // Tests for RequestEnable2Fa method - 200, 400, 401, 500 responses

        #region RequestEnable2Fa Tests

        [Test]
        public async Task RequestEnable2Fa_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var request = new Request2FaDto { CurrentPassword = "valid-password" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.RequestEnable2FaOtpAsync(It.Is<Request2FaDto>(r => r.UserId == userId)))
                .ReturnsAsync(CustomCode.OtpSentSuccessfully);

            var result = await _controller.RequestEnable2Fa(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task RequestEnable2Fa_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new Request2FaDto();

            // Setup fake user to avoid null principal
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }))
                }
            };

            _controller.ModelState.AddModelError("CurrentPassword", "CurrentPassword is required");

            var result = await _controller.RequestEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task RequestEnable2Fa_ShouldReturn401_WhenUserIdMissing()
        {
            var request = new Request2FaDto
            {
                CurrentPassword = "valid-password"
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.RequestEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task RequestEnable2Fa_ShouldReturn500_WhenUnhandledExceptionThrown()
        {
            var userId = Guid.NewGuid();
            var request = new Request2FaDto { CurrentPassword = "valid-password" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
    }));

            _authServiceMock.Setup(s => s.RequestEnable2FaOtpAsync(It.IsAny<Request2FaDto>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.RequestEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for ConfirmEnable2Fa method - 200, 400, 401, 500 responses

        #region ConfirmEnable2Fa Tests

        [Test]
        public async Task ConfirmEnable2Fa_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var request = new Confirm2FaDto { OtpCode = "123456" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.ConfirmEnable2FaOtpAsync(It.Is<Confirm2FaDto>(r => r.UserId == userId)))
                .ReturnsAsync(CustomCode.Success);

            var result = await _controller.ConfirmEnable2Fa(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ConfirmEnable2Fa_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new Confirm2FaDto();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }))
                }
            };

            _controller.ModelState.AddModelError("OtpCode", "OtpCode is required");

            var result = await _controller.ConfirmEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task ConfirmEnable2Fa_ShouldReturn401_WhenUserIdMissing()
        {
            var request = new Confirm2FaDto { OtpCode = "123456" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.ConfirmEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task ConfirmEnable2Fa_ShouldReturn500_WhenUnhandledExceptionThrown()
        {
            var userId = Guid.NewGuid();
            var request = new Confirm2FaDto { OtpCode = "123456" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.ConfirmEnable2FaOtpAsync(It.IsAny<Confirm2FaDto>()))
                .ThrowsAsync(new Exception("Unexpected"));

            var result = await _controller.ConfirmEnable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for ConfirmDisable2Fa method - 200, 400, 401, 500 responses

        #region ConfirmDisable2Fa Tests

        [Test]
        public async Task ConfirmDisable2Fa_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var request = new Confirm2FaDto { OtpCode = "654321" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.ConfirmDisable2FaOtpAsync(It.Is<Confirm2FaDto>(r => r.UserId == userId)))
                .ReturnsAsync(CustomCode.Success);

            var result = await _controller.ConfirmDisable2Fa(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ConfirmDisable2Fa_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new Confirm2FaDto();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }))
                }
            };

            _controller.ModelState.AddModelError("OtpCode", "OtpCode is required");

            var result = await _controller.ConfirmDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task ConfirmDisable2Fa_ShouldReturn401_WhenUserIdMissing()
        {
            var request = new Confirm2FaDto { OtpCode = "654321" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.ConfirmDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task ConfirmDisable2Fa_ShouldReturn500_WhenUnhandledExceptionThrown()
        {
            var userId = Guid.NewGuid();
            var request = new Confirm2FaDto { OtpCode = "654321" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.ConfirmDisable2FaOtpAsync(It.IsAny<Confirm2FaDto>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.ConfirmDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for RequestDisable2Fa method - 200, 400, 401, 500 responses

        #region RequestDisable2Fa Tests

        [Test]
        public async Task RequestDisable2Fa_ShouldReturn200_WhenSuccessful()
        {
            var userId = Guid.NewGuid();
            var request = new Request2FaDto { CurrentPassword = "valid-password" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.RequestDisable2FaOtpAsync(It.Is<Request2FaDto>(r => r.UserId == userId)))
                .ReturnsAsync(CustomCode.OtpSentSuccessfully);

            var result = await _controller.RequestDisable2Fa(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task RequestDisable2Fa_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new Request2FaDto();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }))
                }
            };

            _controller.ModelState.AddModelError("CurrentPassword", "CurrentPassword is required");

            var result = await _controller.RequestDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task RequestDisable2Fa_ShouldReturn401_WhenUserIdMissing()
        {
            var request = new Request2FaDto { CurrentPassword = "valid-password" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await _controller.RequestDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task RequestDisable2Fa_ShouldReturn500_WhenUnhandledExceptionOccurs()
        {
            var userId = Guid.NewGuid();
            var request = new Request2FaDto { CurrentPassword = "valid-password" };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));

            _authServiceMock.Setup(s => s.RequestDisable2FaOtpAsync(It.IsAny<Request2FaDto>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.RequestDisable2Fa(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        // Tests for ResendOtp method - 200, 400, 500 responses

        #region ResendOtp Tests

        [Test]
        public async Task ResendOtp_ShouldReturn200_WhenSuccessful()
        {
            var request = new ResendOtpRequestDto
            {
                Email = "user@example.com",
            };

            _authServiceMock.Setup(s => s.ResendOtpAsync(request))
                .ReturnsAsync(CustomCode.OtpSentSuccessfully);

            var result = await _controller.ResendOtp(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ResendOtp_ShouldReturn400_WhenModelStateInvalid()
        {
            var request = new ResendOtpRequestDto(); // missing Email

            _controller.ModelState.AddModelError("Email", "Email is required");

            var result = await _controller.ResendOtp(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task ResendOtp_ShouldReturn500_WhenUnhandledExceptionThrown()
        {
            var request = new ResendOtpRequestDto
            {
                Email = "error@example.com"
            };

            _authServiceMock.Setup(s => s.ResendOtpAsync(request))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.ResendOtp(request);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

    }
}