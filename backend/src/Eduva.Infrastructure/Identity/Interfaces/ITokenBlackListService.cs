namespace Eduva.Infrastructure.Identity.Interfaces
{
    public interface ITokenBlackListService
    {
        Task BlacklistTokenAsync(string token, DateTimeOffset expiry);
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task BlacklistAllUserTokensAsync(string userId);
        Task<bool> AreUserTokensInvalidatedAsync(string userId, DateTimeOffset tokenIssuedAt);
    }
}