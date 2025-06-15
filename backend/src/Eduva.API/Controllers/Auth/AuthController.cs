using Eduva.API.Controllers.Base;
using Eduva.Application.Common.Exceptions;
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
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }

            try
            {
                var result = await _authService.RegisterAsync(request);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }

                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }

            try
            {
                var (code, result) = await _authService.LoginAsync(request);
                return Respond(code, result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto forgotPasswordRequestDto)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }
            try
            {
                var result = await _authService.ForgotPasswordAsync(forgotPasswordRequestDto);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordRequestDto)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }
            try
            {
                var result = await _authService.ResetPasswordAsync(resetPasswordRequestDto);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmEmailRequestDto request)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }
            try
            {
                var result = await _authService.ConfirmEmailAsync(request);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto changePasswordRequestDto)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Respond(CustomCode.Unauthorized, "User ID not found in claims.");
            }

            changePasswordRequestDto.UserId = Guid.Parse(userId);

            try
            {
                var result = await _authService.ChangePasswordAsync(changePasswordRequestDto);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("resend-confirmation-email")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequestDto resendConfirmationEmailRequestDto)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }
            try
            {
                var result = await _authService.ResendConfirmationEmailAsync(resendConfirmationEmailRequestDto);
                return Respond(result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequestDto)
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }
            try
            {
                var (code, result) = await _authService.RefreshTokenAsync(refreshTokenRequestDto);
                return Respond(code, result);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var modelStateCheck = CheckModelStateValidity();
            if (modelStateCheck != null)
            {
                return modelStateCheck;
            }

            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Respond(CustomCode.Unauthorized, "User ID not found in claims.");
            }

            try
            {
                await _authService.LogoutAsync(userId, token);
                return Respond(CustomCode.Success);
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        // Admin endpoints for token management
        [HttpPost("admin/invalidate-user-tokens/{userId}")]     
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> InvalidateUserTokens(string userId)
        {
            try
            {
                await _authService.InvalidateAllUserTokensAsync(userId);
                return Respond(CustomCode.Success, "All tokens for the user have been invalidated");
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("security/invalidate-all-sessions")]
        [Authorize]
        public async Task<IActionResult> InvalidateAllSessions()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Respond(CustomCode.Unauthorized, "User ID not found in claims.");
            }

            try
            {
                await _authService.InvalidateAllUserTokensAsync(userId);
                return Respond(CustomCode.Success, "All your sessions have been invalidated. Please login again.");
            }
            catch (Exception ex)
            {
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError);
            }
        }
    }
}
