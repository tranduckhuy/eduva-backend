using Eduva.Infrastructure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Eduva.Infrastructure.Test.Services;

[TestFixture]
public class TokenBlackListServiceTests
{
    private TokenBlackListService _service;
    private Mock<IDistributedCache> _cacheMock;
    private Mock<ILogger<TokenBlackListService>> _loggerMock;

    #region TokenBlackListService Setup

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<TokenBlackListService>>();
        _service = new TokenBlackListService(_cacheMock.Object, _loggerMock.Object);
    }

    #endregion

    #region TokenBlackListService Tests

    // Verifies that the token is blacklisted correctly with a positive TTL
    [Test]
    public async Task BlacklistTokenAsync_ShouldSetToken_WhenTtlPositive()
    {
        var token = "sample-token";
        var expiry = DateTime.UtcNow.AddMinutes(10);

        await _service.BlacklistTokenAsync(token, expiry);

        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(k => k == $"blacklist:{token}"),
            It.Is<byte[]>(v => Encoding.UTF8.GetString(v) == "blacklisted"),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    // Verifies that the token is not set when TTL is zero or negative
    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnTrue_IfTokenExists()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("blacklisted"));

        var result = await _service.IsTokenBlacklistedAsync("any-token");

        Assert.That(result, Is.True);
    }

    // Verifies that the method returns false if the token does not exist
    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_IfTokenNotExists()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((byte[]?)null);

        var result = await _service.IsTokenBlacklistedAsync("any-token");

        Assert.That(result, Is.False);
    }

    // Verifies that the method handles exceptions gracefully and returns false
    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_IfExceptionThrown()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ThrowsAsync(new Exception("Cache error"));

        var result = await _service.IsTokenBlacklistedAsync("error-token");

        Assert.That(result, Is.False);
    }

    // Verifies that the exception token is retrieved correctly
    [Test]
    public async Task GetExceptionTokenAsync_ShouldReturnStoredToken()
    {
        _cacheMock.Setup(c => c.GetAsync("user_tokens_exception_user123", default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("exception-token"));

        var result = await _service.GetExceptionTokenAsync("user123");
        Assert.That(result, Is.EqualTo("exception-token"));
    }

    // Verifies that the method returns null if no exception token is stored
    [Test]
    public async Task BlacklistAllUserTokensAsync_ShouldSetInvalidationTime_AndRemoveException()
    {
        await _service.BlacklistAllUserTokensAsync("user123");

        _cacheMock.Verify(c => c.SetAsync(
            "user_tokens_invalidated_user123",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);

        _cacheMock.Verify(c => c.RemoveAsync("user_tokens_exception_user123", default), Times.Once);
    }

    // Verifies that the method sets both keys when blacklisting all user tokens except one
    [Test]
    public async Task BlacklistAllUserTokensExceptAsync_ShouldSetBothKeys()
    {
        await _service.BlacklistAllUserTokensExceptAsync("user123", "current-token");

        _cacheMock.Verify(c => c.SetAsync("user_tokens_invalidated_except_user123",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);

        _cacheMock.Verify(c => c.SetAsync("user_tokens_exception_user123",
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "current-token"),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    // Verifies that the method sets the invalidation key when the token is null or whitespace
    [Test]
    public async Task BlacklistAllUserTokensExceptAsync_ShouldFallback_WhenTokenIsNullOrWhiteSpace()
    {
        await _service.BlacklistAllUserTokensExceptAsync("user123", "");

        _cacheMock.Verify(c => c.SetAsync("user_tokens_invalidated_user123",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    // Verifies that the method checks if user tokens are invalidated correctly
    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnTrue_IfIssuedBeforeInvalidation()
    {
        var invalidationTime = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds().ToString();
        _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("invalidated")), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes(invalidationTime));

        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-10);
        var result = await _service.AreUserTokensInvalidatedAsync("user123", tokenIssuedAt);

        Assert.That(result, Is.True);
    }

    // Verifies that the method returns false if the token was issued after invalidation
    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfIssuedAfterInvalidation()
    {
        var invalidationTime = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        _cacheMock.Setup(c => c.GetAsync(It.Is<string>(s => s.Contains("invalidated")), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes(invalidationTime));

        var tokenIssuedAt = DateTime.UtcNow.AddMinutes(-5);
        var result = await _service.AreUserTokensInvalidatedAsync("user123", tokenIssuedAt);

        Assert.That(result, Is.False);
    }

    // Verifies that the method returns false if no invalidation data is found
    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfInvalidFormat()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("not-a-number"));

        var tokenIssuedAt = DateTime.UtcNow;
        var result = await _service.AreUserTokensInvalidatedAsync("user123", tokenIssuedAt);

        Assert.That(result, Is.False);
    }

    // Verifies that the method returns false if an exception occurs while checking invalidation
    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfExceptionThrown()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ThrowsAsync(new Exception("Redis error"));

        var result = await _service.AreUserTokensInvalidatedAsync("user123", DateTime.UtcNow);

        Assert.That(result, Is.False);
    }

    // Verifies that the method returns false if invalidation data is empty
    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfInvalidationDataIsEmpty()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Array.Empty<byte>());

        var result = await _service.AreUserTokensInvalidatedAsync("user123", DateTime.UtcNow);

        Assert.That(result, Is.False);
    }

    #endregion

}