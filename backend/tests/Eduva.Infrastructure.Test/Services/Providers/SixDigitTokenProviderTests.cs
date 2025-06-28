using Eduva.Application.Common.Exceptions;
using Eduva.Infrastructure.Identity.Providers;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Eduva.Infrastructure.Test.Services.Providers
{
    [TestFixture]
    public class SixDigitTokenProviderTests
    {
        private Mock<UserManager<object>> _userManagerMock;
        private Mock<ILogger<SixDigitTokenProvider<object>>> _loggerMock;
        private SixDigitTokenProvider<object> _provider;

        private readonly TimeSpan _lifespan = TimeSpan.FromSeconds(120);

        #region SixDigitTokenProviderTests Setup

        [SetUp]
        public void SetUp()
        {
            _userManagerMock = new Mock<UserManager<object>>(
                Mock.Of<IUserStore<object>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            var options = Options.Create(new SixDigitTokenProviderOptions
            {
                TokenLifespan = _lifespan
            });

            _loggerMock = new Mock<ILogger<SixDigitTokenProvider<object>>>();
            _provider = new SixDigitTokenProvider<object>(options, _loggerMock.Object);
        }

        #endregion

        #region SixDigitTokenProvider Tests

        [Test]
        public void CanInstantiate_SixDigitTokenProviderOptions()
        {
            var options = new SixDigitTokenProviderOptions();
            Assert.That(options, Is.Not.Null);
        }

        [Test]
        public async Task GenerateAsync_ShouldStoreOtpClaims()
        {
            var user = new object();

            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _userManagerMock.Setup(x => x.AddClaimAsync(user, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.RemoveClaimAsync(user, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

            var otp = await _provider.GenerateAsync("login", _userManagerMock.Object, user);

            Assert.Multiple(() =>
            {
                Assert.That(otp, Has.Length.EqualTo(6));
                Assert.That(int.TryParse(otp, out _), Is.True);
            });
        }

        [Test]
        public async Task ValidateAsync_ShouldReturnTrue_WhenOtpMatches()
        {
            var otp = "123456";
            var user = new object();

            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
            {
                new("LastOtpValue", otp),
                new("LastOtpSentTime", DateTime.UtcNow.ToString("o"))
            });

            var result = await _provider.ValidateAsync("login", otp, _userManagerMock.Object, user);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ValidateAsync_ShouldReturnFalse_WhenOtpExpired()
        {
            var otp = "123456";
            var user = new object();

            var expired = DateTime.UtcNow.Subtract(_lifespan).AddSeconds(-1);
            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
            {
                new("LastOtpValue", otp),
                new("LastOtpSentTime", expired.ToString("o"))
            });

            var result = await _provider.ValidateAsync("login", otp, _userManagerMock.Object, user);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CheckOtpThrottleAsync_ShouldThrow_IfRequestedTooSoon()
        {
            var user = new object();
            var sentTime = DateTime.UtcNow;

            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>
            {
                new("LastOtpSentTime", sentTime.ToString("o"))
            });

            var ex = Assert.ThrowsAsync<AppException>(() =>
                _provider.CheckOtpThrottleAsync(_userManagerMock.Object, user));

            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.OtpResendTooSoon));
        }

        [Test]
        public async Task ForceClearOtpClaimsAsync_ShouldRemove_OtpClaims()
        {
            var user = new object();
            var claims = new List<Claim>
            {
                new("LastOtpValue", "123456"),
                new("LastOtpSentTime", DateTime.UtcNow.ToString("o"))
            };

            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(claims);
            _userManagerMock.Setup(x => x.RemoveClaimAsync(user, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

            await _provider.ForceClearOtpClaimsAsync(_userManagerMock.Object, user);

            _userManagerMock.Verify(x => x.RemoveClaimAsync(user, It.IsAny<Claim>()), Times.Exactly(2));
        }

        #endregion

    }
}