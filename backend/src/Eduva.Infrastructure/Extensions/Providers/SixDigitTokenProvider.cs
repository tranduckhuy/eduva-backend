using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Eduva.Infrastructure.Extensions.Providers
{
    public class SixDigitTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
        where TUser : class
    {
        private readonly TimeSpan _tokenLifespan;
        private readonly ILogger<SixDigitTokenProvider<TUser>> _logger;

        public SixDigitTokenProvider(IOptions<SixDigitTokenProviderOptions> options, ILogger<SixDigitTokenProvider<TUser>> logger)
        {
            _tokenLifespan = options.Value.TokenLifespan;
            _logger = logger;
        }

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
            => Task.FromResult(true);

        public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var claims = await manager.GetClaimsAsync(user);

            // Remove old OTP-related claims
            var oldClaims = claims.Where(c => c.Type is "LastOtpSentTime" or "LastOtpValue").ToList();
            foreach (var old in oldClaims)
            {
                var result = await manager.RemoveClaimAsync(user, old);
                _logger.LogInformation("Removed old claim {Type}: {Success}", old.Type, result.Succeeded);
            }

            var now = DateTime.UtcNow;
            var sentTimeClaim = new Claim("LastOtpSentTime", now.ToString("o"));
            var otpValueClaim = new Claim("LastOtpValue", otp);

            var result1 = await manager.AddClaimAsync(user, sentTimeClaim);
            var result2 = await manager.AddClaimAsync(user, otpValueClaim);

            _logger.LogInformation("Generated OTP: {Otp} at {Time}. Saved claims success: Time={Success1}, Value={Success2}",
                otp, now, result1.Succeeded, result2.Succeeded);

            return otp;
        }

        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            if (!await IsTokenStillValidAsync(manager, user))
            {
                _logger.LogWarning("OTP expired or missing throttle claim.");
                return false;
            }

            var claims = await manager.GetClaimsAsync(user);
            var otpClaim = claims.FirstOrDefault(c => c.Type == "LastOtpValue");

            var isMatch = otpClaim?.Value == token;
            _logger.LogInformation("OTP validation result: {Result}", isMatch);
            return isMatch;
        }

        public async Task CheckOtpThrottleAsync(UserManager<TUser> manager, TUser user)
        {
            var claims = await manager.GetClaimsAsync(user);
            var claim = claims.FirstOrDefault(c => c.Type == "LastOtpSentTime");

            if (claim is null)
            {
                _logger.LogInformation("No LastOtpSentTime claim found → Allow sending OTP.");
                return;
            }

            try
            {
                var lastSent = DateTime.ParseExact(
                    claim.Value,
                    "o",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
                );

                var now = DateTime.UtcNow;

                if (now < lastSent.Add(_tokenLifespan))
                {
                    var wait = lastSent.Add(_tokenLifespan) - now;
                    _logger.LogWarning("OTP requested too soon. Wait {Seconds} more seconds.", wait.TotalSeconds);
                    throw new AppException(CustomCode.OtpResendTooSoon);
                }

                _logger.LogInformation("Throttle passed. Now={Now}, LastSent={LastSent}, ExpireAt={Expire}", now, lastSent, lastSent.Add(_tokenLifespan));
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid datetime format in LastOtpSentTime: {Value}", claim.Value);
                return;
            }
        }

        private async Task<bool> IsTokenStillValidAsync(UserManager<TUser> manager, TUser user)
        {
            var claims = await manager.GetClaimsAsync(user);
            var claim = claims.FirstOrDefault(c => c.Type == "LastOtpSentTime");

            if (claim == null) return false;

            try
            {
                var lastSent = DateTime.ParseExact(
                    claim.Value,
                    "o",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
                );

                var now = DateTime.UtcNow;

                var isValid = now <= lastSent.Add(_tokenLifespan);

                _logger.LogInformation("Token validity: {Valid} (Now: {Now}, Sent: {Sent}, ExpiresAt: {Expires})",
                    isValid, now, lastSent, lastSent.Add(_tokenLifespan));

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LastOtpSentTime.");
                return false;
            }
        }

        public async Task ForceClearOtpClaimsAsync(UserManager<TUser> manager, TUser user)
        {
            var claims = await manager.GetClaimsAsync(user);
            var otpClaims = claims.Where(c => c.Type is "LastOtpSentTime" or "LastOtpValue");

            foreach (var claim in otpClaims)
            {
                await manager.RemoveClaimAsync(user, claim);
            }
        }
    }
}