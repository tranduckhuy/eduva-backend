using Eduva.API.Controllers.Base;
using Eduva.Application.Features.Auth.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Auth
{
    [Route("api/auth")]
    public class AuthController : BaseController<AuthController>
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger) : base(logger)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            return await HandleRequestAsync(() => _authService.RegisterAsync(request));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            return await HandleRequestAsync(async () =>
            {
                var (code, result) = await _authService.LoginAsync(request);
                return (code, result);
            });
        }

        [HttpPost("verify-otp-login")]
        public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyOtpRequestDto request)
        {
            return await HandleRequestAsync(async () =>
            {
                var (code, result) = await _authService.VerifyLoginOtpAsync(request);
                return (code, result);
            });
        }

        [HttpPost("security/request-enable-2fa")]
        [Authorize]
        public async Task<IActionResult> RequestEnable2Fa([FromBody] Request2FaDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            request.UserId = id;
            return await HandleRequestAsync(() => _authService.RequestEnable2FaOtpAsync(request));
        }

        [HttpPost("security/confirm-enable-2fa")]
        [Authorize]
        public async Task<IActionResult> ConfirmEnable2Fa([FromBody] Confirm2FaDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            request.UserId = id;
            return await HandleRequestAsync(() => _authService.ConfirmEnable2FaOtpAsync(request));
        }

        [HttpPost("security/request-disable-2fa")]
        [Authorize]
        public async Task<IActionResult> RequestDisable2Fa([FromBody] Request2FaDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            request.UserId = id;
            return await HandleRequestAsync(() => _authService.RequestDisable2FaOtpAsync(request));
        }

        [HttpPost("security/confirm-disable-2fa")]
        [Authorize]
        public async Task<IActionResult> ConfirmDisable2Fa([FromBody] Confirm2FaDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            request.UserId = id;
            return await HandleRequestAsync(() => _authService.ConfirmDisable2FaOtpAsync(request));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            return await HandleRequestAsync(() => _authService.ForgotPasswordAsync(dto));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            return await HandleRequestAsync(() => _authService.ResetPasswordAsync(dto));
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmEmailRequestDto request)
        {
            return await HandleRequestAsync(() => _authService.ConfirmEmailAsync(request));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            dto.UserId = id;
            var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Respond(CustomCode.AccessTokenInvalidOrExpired);
            }
            dto.CurrentAccessToken = token;
            return await HandleRequestAsync(() => _authService.ChangePasswordAsync(dto));
        }

        [HttpPost("resend-confirmation-email")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequestDto dto)
        {
            return await HandleRequestAsync(() => _authService.ResendConfirmationEmailAsync(dto));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            return await HandleRequestAsync(async () =>
            {
                var (code, result) = await _authService.RefreshTokenAsync(dto);
                return (code, result);
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            return await HandleRequestAsync(() => _authService.LogoutAsync(userId, token));
        }

        [HttpPost("admin/invalidate-user-tokens/{userId}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> InvalidateUserTokens(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            return await HandleRequestAsync(() => _authService.InvalidateAllUserTokensAsync(userId));
        }

        [HttpPost("security/invalidate-all-sessions")]
        [Authorize]
        public async Task<IActionResult> InvalidateAllSessions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }
            return await HandleRequestAsync(() => _authService.InvalidateAllUserTokensAsync(userId));
        }
    }
}