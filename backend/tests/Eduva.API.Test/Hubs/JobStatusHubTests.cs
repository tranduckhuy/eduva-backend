using Eduva.API.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Hubs
{
    [TestFixture]
    public class JobStatusHubTests
    {
        private Mock<ILogger<JobStatusHub>> _loggerMock = null!;
        private JobStatusHub _hub = null!;
        private Mock<HubCallerContext> _contextMock = null!;
        private Mock<ClaimsPrincipal> _userMock = null!;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<JobStatusHub>>();
            _hub = new JobStatusHub(_loggerMock.Object);
            _contextMock = new Mock<HubCallerContext>();
            _userMock = new Mock<ClaimsPrincipal>();
        }

        [TearDown]
        public void TearDown()
        {
            _hub?.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldCreateInstance_WithValidLogger()
        {
            var hub = new JobStatusHub(_loggerMock.Object);
            Assert.That(hub, Is.Not.Null);
            hub.Dispose();
        }

        [Test]
        public void Constructor_ShouldCreateInstance_WithNullLogger()
        {
            // JobStatusHub constructor doesn't validate logger parameter
            var hub = new JobStatusHub(null!);
            Assert.That(hub, Is.Not.Null);
            hub.Dispose();
        }

        [Test]
        public void JobStatusHub_ShouldHaveAuthorizeAttribute()
        {
            var authorizeAttribute = typeof(JobStatusHub).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .FirstOrDefault() as AuthorizeAttribute;
            Assert.That(authorizeAttribute, Is.Not.Null);
        }

        #endregion

        #region OnConnectedAsync Tests

        [Test]
        public async Task OnConnectedAsync_ShouldLogUserConnection_WhenUserIsAuthenticated()
        {
            // Arrange
            var userId = "test-user-id";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            _userMock.Setup(u => u.FindFirst(ClaimTypes.NameIdentifier)).Returns(claims.First());
            _contextMock.Setup(c => c.User).Returns(_userMock.Object);
            _hub.Context = _contextMock.Object;

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} connected to JobStatusHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Test]
        public async Task OnConnectedAsync_ShouldLogUserConnection_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
            _hub.Context = _contextMock.Object;

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User (null) connected to JobStatusHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion

        #region OnDisconnectedAsync Tests

        [Test]
        public async Task OnDisconnectedAsync_ShouldLogUserDisconnection_WhenUserIsAuthenticated()
        {
            // Arrange
            var userId = "test-user-id";
            _contextMock.Setup(c => c.UserIdentifier).Returns(userId);
            _hub.Context = _contextMock.Object;

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userId} disconnected from JobStatusHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Test]
        public async Task OnDisconnectedAsync_ShouldLogUserDisconnection_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _contextMock.Setup(c => c.UserIdentifier).Returns((string?)null);
            _hub.Context = _contextMock.Object;

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User (null) disconnected from JobStatusHub")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion
    }
}