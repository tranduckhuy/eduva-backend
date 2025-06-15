namespace Eduva.Infrastructure.Identity.Interfaces
{
    public interface ITokenBlackListService
    {
        Task BlacklistTokenAsync(string token, DateTime expiry);
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task<string?> GetExceptionTokenAsync(string userId);
        Task BlacklistAllUserTokensExceptAsync(string userId, string currentToken);
        Task BlacklistAllUserTokensAsync(string userId);
        Task<bool> AreUserTokensInvalidatedAsync(string userId, DateTime tokenIssuedAt);
    }
}