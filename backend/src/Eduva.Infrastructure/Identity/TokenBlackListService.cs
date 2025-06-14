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

        public async Task BlacklistTokenAsync(string token, DateTime expiry)
        {
            var key = $"blacklist:{token}";
            var ttl = (expiry - DateTime.UtcNow).TotalSeconds;

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
            var invalidationTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var userTokenKey = $"user_tokens_invalidated_{userId}";
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };

            // Store as Unix timestamp to avoid timezone issues
            await _distributedCache.SetStringAsync(userTokenKey, invalidationTime.ToString(), cacheOptions);
            
            _logger.LogInformation("All tokens for user {UserId} have been blacklisted at {InvalidationTime}", userId, DateTimeOffset.FromUnixTimeSeconds(invalidationTime));
        }

        public async Task<bool> AreUserTokensInvalidatedAsync(string userId, DateTime tokenIssuedAt)
        {
            try
            {
                var userTokenKey = $"user_tokens_invalidated_{userId}";
                var invalidationData = await _distributedCache.GetStringAsync(userTokenKey);
                
                if (string.IsNullOrEmpty(invalidationData))
                    return false;

                long invalidationTimeUnix;
                
                // Handle backward compatibility - check if it's old DateTime format or new Unix timestamp
                if (invalidationData.StartsWith("\"") && invalidationData.EndsWith("\""))
                {
                    var dateTimeString = invalidationData.Trim('"');
                    if (DateTime.TryParse(dateTimeString, out var invalidationDateTime))
                    {
                        invalidationTimeUnix = new DateTimeOffset(invalidationDateTime.ToUniversalTime()).ToUnixTimeSeconds();
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse old DateTime format for user {UserId}: {Data}", userId, invalidationData);
                        return false;
                    }
                }
                else
                {
                    if (!long.TryParse(invalidationData, out invalidationTimeUnix))
                    {
                        _logger.LogWarning("Failed to parse Unix timestamp for user {UserId}: {Data}", userId, invalidationData);
                        return false;
                    }
                }

                var tokenIssuedAtUnix = new DateTimeOffset(tokenIssuedAt.ToUniversalTime()).ToUnixTimeSeconds();
                
                return tokenIssuedAtUnix < invalidationTimeUnix;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user tokens are invalidated");
                return false;
            }
        }
    }
}