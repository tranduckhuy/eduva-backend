using Eduva.API.Adapters;
using Eduva.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Eduva.API.Test.Adapters
{
    [TestFixture]
    public class SignalRNotificationHubTests
    {
        #region Fields

        private SignalRNotificationHub _hub;
        private Mock<IHubContext<NotificationHub>> _notificationHubContextMock;
        private Mock<IHubContext<JobStatusHub>> _jobStatusHubContextMock;
        private Mock<IClientProxy> _clientProxyMock;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            _notificationHubContextMock = new Mock<IHubContext<NotificationHub>>();
            _jobStatusHubContextMock = new Mock<IHubContext<JobStatusHub>>();
            _clientProxyMock = new Mock<IClientProxy>();

            _hub = new SignalRNotificationHub(
                _notificationHubContextMock.Object,
                _jobStatusHubContextMock.Object);
        }

        #endregion

        #region SendNotificationToGroupAsync Tests

        [Test]
        public async Task SendNotificationToGroupAsync_WithJobGroup_ShouldSendToJobStatusHub()
        {
            // Arrange
            var groupName = "job_123";
            var eventName = "JobStatusUpdated";
            var data = new { Status = "Completed" };

            _jobStatusHubContextMock.Setup(x => x.Clients.Group(groupName))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToGroupAsync(groupName, eventName, data);

            // Assert
            _jobStatusHubContextMock.Verify(x => x.Clients.Group(groupName), Times.Once);
            _notificationHubContextMock.Verify(x => x.Clients.Group(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendNotificationToGroupAsync_WithNonJobGroup_ShouldSendToNotificationHub()
        {
            // Arrange
            var groupName = "school_456";
            var eventName = "NewLessonMaterial";
            var data = new { Title = "Math Lesson" };

            _notificationHubContextMock.Setup(x => x.Clients.Group(groupName))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToGroupAsync(groupName, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.Group(groupName), Times.Once);
            _jobStatusHubContextMock.Verify(x => x.Clients.Group(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendNotificationToGroupAsync_WithEmptyGroupName_ShouldHandleGracefully()
        {
            // Arrange
            var groupName = string.Empty;
            var eventName = "TestEvent";
            var data = new { Test = "data" };

            _notificationHubContextMock.Setup(x => x.Clients.Group(groupName))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToGroupAsync(groupName, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.Group(groupName), Times.Once);
        }

        #endregion

        #region SendNotificationToUserAsync Tests

        [Test]
        public async Task SendNotificationToUserAsync_WithJobEvent_ShouldSendToJobStatusHub()
        {
            // Arrange
            var userId = "user123";
            var eventName = "JobStatusUpdated";
            var data = new { JobId = Guid.NewGuid() };

            _jobStatusHubContextMock.Setup(x => x.Clients.User(userId))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToUserAsync(userId, eventName, data);

            // Assert
            _jobStatusHubContextMock.Verify(x => x.Clients.User(userId), Times.Once);
            _notificationHubContextMock.Verify(x => x.Clients.User(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendNotificationToUserAsync_WithNonJobEvent_ShouldSendToNotificationHub()
        {
            // Arrange
            var userId = "user123";
            var eventName = "NewNotification";
            var data = new { Message = "Hello" };

            _notificationHubContextMock.Setup(x => x.Clients.User(userId))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToUserAsync(userId, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.User(userId), Times.Once);
            _jobStatusHubContextMock.Verify(x => x.Clients.User(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SendNotificationToUserAsync_WithEmptyUserId_ShouldHandleGracefully()
        {
            // Arrange
            var userId = string.Empty;
            var eventName = "TestEvent";
            var data = new { Test = "data" };

            _notificationHubContextMock.Setup(x => x.Clients.User(userId))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToUserAsync(userId, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.User(userId), Times.Once);
        }

        #endregion

        #region SendNotificationToAllAsync Tests

        [Test]
        public async Task SendNotificationToAllAsync_ShouldSendToNotificationHub()
        {
            // Arrange
            var eventName = "SystemBroadcast";
            var data = new { Message = "System maintenance" };

            _notificationHubContextMock.Setup(x => x.Clients.All)
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToAllAsync(eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.All, Times.Once);
        }

        [Test]
        public async Task SendNotificationToAllAsync_WithNullData_ShouldHandleGracefully()
        {
            // Arrange
            var eventName = "TestEvent";
            object data = null!;

            _notificationHubContextMock.Setup(x => x.Clients.All)
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToAllAsync(eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.All, Times.Once);
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task SendNotificationToGroupAsync_WithNullData_ShouldHandleGracefully()
        {
            // Arrange
            var groupName = "test_group";
            var eventName = "TestEvent";
            object data = null!;

            _notificationHubContextMock.Setup(x => x.Clients.Group(groupName))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToGroupAsync(groupName, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.Group(groupName), Times.Once);
        }

        [Test]
        public async Task SendNotificationToUserAsync_WithNullData_ShouldHandleGracefully()
        {
            // Arrange
            var userId = "user123";
            var eventName = "TestEvent";
            object data = null!;

            _notificationHubContextMock.Setup(x => x.Clients.User(userId))
                .Returns(_clientProxyMock.Object);

            // Act
            await _hub.SendNotificationToUserAsync(userId, eventName, data);

            // Assert
            _notificationHubContextMock.Verify(x => x.Clients.User(userId), Times.Once);
        }

        #endregion
    }
}