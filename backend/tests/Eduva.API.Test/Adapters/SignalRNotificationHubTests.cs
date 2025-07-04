using Eduva.API.Adapters;
using Eduva.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Eduva.API.Test.Adapters
{
    [TestFixture]
    public class SignalRNotificationHubTests
    {
        private Mock<IHubContext<QuestionCommentHub>> _mockHubContext;
        private Mock<IHubClients> _mockClients;
        private Mock<IClientProxy> _mockClientProxy;
        private SignalRNotificationHub _signalRNotificationHub;

        #region Setup and TearDown

        [SetUp]
        public void Setup()
        {
            _mockHubContext = new Mock<IHubContext<QuestionCommentHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);

            _signalRNotificationHub = new SignalRNotificationHub(_mockHubContext.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _mockHubContext?.Reset();
            _mockClients?.Reset();
            _mockClientProxy?.Reset();
        }

        #endregion

        #region Tests

        [Test]
        public void Constructor_ShouldInitializeHubContext()
        {
            // Arrange & Act
            var hub = new SignalRNotificationHub(_mockHubContext.Object);

            // Assert
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub, Is.InstanceOf<SignalRNotificationHub>());
        }

        [Test]
        public async Task SendNotificationToGroupAsync_ShouldCallGroupSendAsync()
        {
            // Arrange
            var groupName = "test-group";
            var eventName = "test-event";
            var data = new { message = "test data" };

            _mockClients.Setup(x => x.Group(groupName)).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToGroupAsync(groupName, eventName, data);

            // Assert
            _mockClients.Verify(x => x.Group(groupName), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == data),
                default), Times.Once);
        }

        [Test]
        public async Task SendNotificationToUserAsync_ShouldCallUserSendAsync()
        {
            // Arrange
            var userId = "user-123";
            var eventName = "user-event";
            var data = new { message = "user notification" };

            _mockClients.Setup(x => x.User(userId)).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToUserAsync(userId, eventName, data);

            // Assert
            _mockClients.Verify(x => x.User(userId), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == data),
                default), Times.Once);
        }

        [Test]
        public async Task SendNotificationToAllAsync_ShouldCallAllSendAsync()
        {
            // Arrange
            var eventName = "broadcast-event";
            var data = new { message = "broadcast to all" };

            _mockClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToAllAsync(eventName, data);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == data),
                default), Times.Once);
        }

        [Test]
        public async Task SendNotificationToGroupAsync_WithNullData_ShouldStillWork()
        {
            // Arrange
            var groupName = "test-group";
            var eventName = "test-event";
            object? data = null;

            _mockClients.Setup(x => x.Group(groupName)).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToGroupAsync(groupName, eventName, data!);

            // Assert
            _mockClients.Verify(x => x.Group(groupName), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == data),
                default), Times.Once);
        }

        [Test]
        public async Task SendNotificationToUserAsync_WithComplexData_ShouldWork()
        {
            // Arrange
            var userId = "user-456";
            var eventName = "complex-event";
            var complexData = new
            {
                id = Guid.NewGuid(),
                title = "Test Notification",
                timestamp = DateTime.UtcNow,
                metadata = new { priority = "high", category = "urgent" }
            };

            _mockClients.Setup(x => x.User(userId)).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToUserAsync(userId, eventName, complexData);

            // Assert
            _mockClients.Verify(x => x.User(userId), Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == complexData),
                default), Times.Once);
        }

        [Test]
        public async Task SendNotificationToAllAsync_WithEmptyEventName_ShouldStillWork()
        {
            // Arrange
            var eventName = "";
            var data = new { message = "empty event name test" };

            _mockClients.Setup(x => x.All).Returns(_mockClientProxy.Object);

            // Act
            await _signalRNotificationHub.SendNotificationToAllAsync(eventName, data);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
            _mockClientProxy.Verify(x => x.SendCoreAsync(eventName,
                It.Is<object[]>(args => args.Length == 1 && args[0] == data),
                default), Times.Once);
        }

        #endregion

    }
}