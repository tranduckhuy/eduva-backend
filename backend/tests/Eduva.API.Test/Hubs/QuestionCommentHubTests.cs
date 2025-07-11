using Eduva.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Hubs
{
    [TestFixture]
    public class QuestionCommentHubTests
    {
        private Mock<ILogger<QuestionCommentHub>> _mockLogger;
        private Mock<HubCallerContext> _mockContext;
        private Mock<IGroupManager> _mockGroups;
        private QuestionCommentHub _hub;

        #region Setup and Teardown

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<QuestionCommentHub>>();
            _mockContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();

            _hub = new QuestionCommentHub(_mockLogger.Object);

            // Setup Hub properties through reflection (since they're protected)
            var contextProperty = typeof(Hub).GetProperty(nameof(Hub.Context));
            contextProperty?.SetValue(_hub, _mockContext.Object);

            var groupsProperty = typeof(Hub).GetProperty(nameof(Hub.Groups));
            groupsProperty?.SetValue(_hub, _mockGroups.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose the hub instance
            _hub?.Dispose();

            // Reset mocks
            _mockLogger?.Reset();
            _mockContext?.Reset();
            _mockGroups?.Reset();
        }

        #endregion

        #region Tests

        [Test]
        public async Task OnDisconnectedAsync_WithUserMissingNameIdentifierClaim_ShouldLogWithNullUserId()
        {
            // Arrange
            var connectionId = "connection-missing-claim";
            var exception = new Exception("Test exception");

            // Create user with different claims but missing NameIdentifier
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnDisconnectedAsync(exception);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User disconnected with error") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains("Test exception")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnDisconnectedAsync_WithUserMissingNameIdentifierClaim_WithoutException_ShouldLogWithNullUserId()
        {
            // Arrange
            var connectionId = "connection-missing-claim-normal";

            // Create user with different claims but missing NameIdentifier
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User disconnected normally") &&
                                                 v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task JoinLessonGroup_WithUserMissingNameIdentifierClaim_ShouldStillWork()
        {
            // Arrange
            var connectionId = "connection-join-missing-claim";
            var lessonMaterialId = "lesson-missing-claim";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            // Create user with different claims but missing NameIdentifier
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.JoinLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.AddToGroupAsync(connectionId, expectedGroupName, default), Times.Once);

            // Verify joining log (userId should be null in the log)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User joining group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName) &&
                                                 v.ToString()!.Contains(lessonMaterialId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify success log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User successfully joined group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task LeaveLessonGroup_WithUserMissingNameIdentifierClaim_ShouldStillWork()
        {
            // Arrange
            var connectionId = "connection-leave-missing-claim";
            var lessonMaterialId = "lesson-leave-missing-claim";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            // Create user with different claims but missing NameIdentifier
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim(ClaimTypes.Email, "test@example.com")
    };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.LeaveLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default), Times.Once);

            // Verify leaving log (userId should be null in the log)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User leaving group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName) &&
                                                 v.ToString()!.Contains(lessonMaterialId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify success log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User successfully left group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void Constructor_ShouldInitializeLogger()
        {
            // Arrange & Act
            var hub = new QuestionCommentHub(_mockLogger.Object);

            // Assert
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub, Is.InstanceOf<QuestionCommentHub>());

            // Dispose the local hub instance
            hub.Dispose();
        }

        [Test]
        public async Task OnConnectedAsync_WithValidUser_ShouldLogUserConnectionInfo()
        {
            // Arrange
            var userId = "user-123";
            var userName = "testuser";
            var connectionId = "connection-456";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName)
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User connected") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(userId) &&
                                                 v.ToString()!.Contains(userName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnConnectedAsync_WithNullUser_ShouldLogWithNullValues()
        {
            // Arrange
            var connectionId = "connection-789";
            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User connected") &&
                                                 v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnConnectedAsync_WithUserMissingClaims_ShouldLogWithNullValues()
        {
            // Arrange
            var connectionId = "connection-000";
            var identity = new ClaimsIdentity(); // Empty claims
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User connected") &&
                                                 v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnDisconnectedAsync_WithException_ShouldLogWarning()
        {
            // Arrange
            var userId = "user-456";
            var connectionId = "connection-789";
            var exception = new Exception("Connection lost");

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnDisconnectedAsync(exception);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User disconnected with error") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(userId) &&
                                                 v.ToString()!.Contains("Connection lost")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnDisconnectedAsync_WithoutException_ShouldLogInformation()
        {
            // Arrange
            var userId = "user-789";
            var connectionId = "connection-012";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User disconnected normally") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(userId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task OnDisconnectedAsync_WithNullUser_ShouldLogWithNullUserId()
        {
            // Arrange
            var connectionId = "connection-null";
            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.OnDisconnectedAsync(null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User disconnected normally") &&
                                                 v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task JoinLessonGroup_ShouldAddToGroupAndLog()
        {
            // Arrange
            var userId = "user-join-123";
            var connectionId = "connection-join-456";
            var lessonMaterialId = "lesson-789";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.JoinLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.AddToGroupAsync(connectionId, expectedGroupName, default), Times.Once);

            // Verify joining log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User joining group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(userId) &&
                                                 v.ToString()!.Contains(expectedGroupName) &&
                                                 v.ToString()!.Contains(lessonMaterialId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify success log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User successfully joined group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task JoinLessonGroup_WithNullUser_ShouldStillWork()
        {
            // Arrange
            var connectionId = "connection-join-null";
            var lessonMaterialId = "lesson-null";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.JoinLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.AddToGroupAsync(connectionId, expectedGroupName, default), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User joining group")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task LeaveLessonGroup_ShouldRemoveFromGroupAndLog()
        {
            // Arrange
            var userId = "user-leave-123";
            var connectionId = "connection-leave-456";
            var lessonMaterialId = "lesson-leave-789";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _mockContext.Setup(x => x.User).Returns(principal);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.LeaveLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default), Times.Once);

            // Verify leaving log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User leaving group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(userId) &&
                                                 v.ToString()!.Contains(expectedGroupName) &&
                                                 v.ToString()!.Contains(lessonMaterialId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify success log
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User successfully left group") &&
                                                 v.ToString()!.Contains(connectionId) &&
                                                 v.ToString()!.Contains(expectedGroupName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task LeaveLessonGroup_WithNullUser_ShouldStillWork()
        {
            // Arrange
            var connectionId = "connection-leave-null";
            var lessonMaterialId = "lesson-leave-null";
            var expectedGroupName = $"Lesson_{lessonMaterialId}";

            _mockContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _hub.LeaveLessonGroup(lessonMaterialId);

            // Assert
            _mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[SignalR] User leaving group")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

    }
}