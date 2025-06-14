using System.Text.Json;
using Eduva.Infrastructure.Identity.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Identity
{
    public class TokenBlackListService : ITokenBlackListService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<TokenBlackListService> _logger;

        public TokenBlackListService(IDistributedCache distributedCache, ILogger<TokenBlackListService> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task BlacklistTokenAsync(string token, DateTimeOffset expiry)
        {
            var key = $"blacklist:{token}";
            var ttl = (expiry - DateTimeOffset.UtcNow).TotalSeconds;

            if (ttl > 0)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl)
                };

                // Save token to cache with time to live
                await _distributedCache.SetStringAsync(key, "blacklisted", options);
            }

            _logger.LogInformation("Token blacklisted until {ExpirationDate}", expiry);
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            try
            {
                var tokenData = await _distributedCache.GetStringAsync($"blacklist:{token}");
                return !string.IsNullOrEmpty(tokenData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is blacklisted");
                return false;
            }
        }

        public async Task BlacklistAllUserTokensAsync(string userId)
        {
            // Store a timestamp when all user tokens were invalidated
            var invalidationTime = DateTime.UtcNow;
            var userTokenKey = $"user_tokens_invalidated_{userId}";
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            await _distributedCache.SetStringAsync(userTokenKey, 
                JsonSerializer.Serialize(invalidationTime), cacheOptions);
            
            _logger.LogInformation("All tokens for user {UserId} have been blacklisted", userId);
        }

        public async Task<bool> AreUserTokensInvalidatedAsync(string userId, DateTimeOffset tokenIssuedAt)
        {
            try
            {
                var userTokenKey = $"user_tokens_invalidated_{userId}";
                var invalidationData = await _distributedCache.GetStringAsync(userTokenKey);
                
                if (string.IsNullOrEmpty(invalidationData))
                    return false;

                var invalidationTime = JsonSerializer.Deserialize<DateTimeOffset>(invalidationData);
                return tokenIssuedAt < invalidationTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user tokens are invalidated");
                return false;
            }
        }
    }
}