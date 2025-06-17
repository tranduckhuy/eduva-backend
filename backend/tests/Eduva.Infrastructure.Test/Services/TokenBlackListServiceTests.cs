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

    [SetUp]
    public void SetUp()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<TokenBlackListService>>();
        _service = new TokenBlackListService(_cacheMock.Object, _loggerMock.Object);
    }

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

    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnTrue_IfTokenExists()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("blacklisted"));

        var result = await _service.IsTokenBlacklistedAsync("any-token");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_IfTokenNotExists()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((byte[]?)null);

        var result = await _service.IsTokenBlacklistedAsync("any-token");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsTokenBlacklistedAsync_ShouldReturnFalse_IfExceptionThrown()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ThrowsAsync(new Exception("Cache error"));

        var result = await _service.IsTokenBlacklistedAsync("error-token");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetExceptionTokenAsync_ShouldReturnStoredToken()
    {
        _cacheMock.Setup(c => c.GetAsync("user_tokens_exception_user123", default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("exception-token"));

        var result = await _service.GetExceptionTokenAsync("user123");
        Assert.That(result, Is.EqualTo("exception-token"));
    }

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

    [Test]
    public async Task BlacklistAllUserTokensExceptAsync_ShouldFallback_WhenTokenIsNullOrWhiteSpace()
    {
        await _service.BlacklistAllUserTokensExceptAsync("user123", "");

        _cacheMock.Verify(c => c.SetAsync("user_tokens_invalidated_user123",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

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

    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfInvalidFormat()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Encoding.UTF8.GetBytes("not-a-number"));

        var tokenIssuedAt = DateTime.UtcNow;
        var result = await _service.AreUserTokensInvalidatedAsync("user123", tokenIssuedAt);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfExceptionThrown()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ThrowsAsync(new Exception("Redis error"));

        var result = await _service.AreUserTokensInvalidatedAsync("user123", DateTime.UtcNow);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AreUserTokensInvalidatedAsync_ShouldReturnFalse_IfInvalidationDataIsEmpty()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                  .ReturnsAsync(Array.Empty<byte>());

        var result = await _service.AreUserTokensInvalidatedAsync("user123", DateTime.UtcNow);

        Assert.That(result, Is.False);
    }
}