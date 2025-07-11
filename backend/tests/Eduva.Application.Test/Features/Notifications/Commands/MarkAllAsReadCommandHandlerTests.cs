using Eduva.Application.Features.Notifications.Commands;
using Eduva.Application.Interfaces.Services;
using Moq;

namespace Eduva.Application.Test.Features.Notifications.Commands
{
    [TestFixture]
    public class MarkAllAsReadCommandHandlerTests
    {
        #region Fields and Setup

        private Mock<INotificationService> _notificationServiceMock;
        private MarkAllAsReadCommandHandler _handler;
        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _handler = new MarkAllAsReadCommandHandler(_notificationServiceMock.Object);
        }

        #endregion

        #region Handle Tests

        [Test]
        public async Task Handle_ShouldReturnTrue_WhenValidUserId()
        {
            // Arrange
            var command = new MarkAllAsReadCommand { UserId = _userId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _notificationServiceMock.Verify(s => s.MarkAllAsReadAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCallService_ExactlyOnce()
        {
            // Arrange
            var command = new MarkAllAsReadCommand { UserId = _userId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                _notificationServiceMock.Verify(s => s.MarkAllAsReadAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
                _notificationServiceMock.VerifyNoOtherCalls();
            });
        }

        [Test]
        public async Task Handle_ShouldPassCorrectParameters_ToService()
        {
            // Arrange
            var command = new MarkAllAsReadCommand { UserId = _userId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _notificationServiceMock.Verify(s => s.MarkAllAsReadAsync(
                It.Is<Guid>(id => id == _userId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public async Task Handle_ShouldReturnTrue_EvenWithEmptyGuid()
        {
            // Arrange
            var command = new MarkAllAsReadCommand { UserId = Guid.Empty };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                _notificationServiceMock.Verify(s => s.MarkAllAsReadAsync(Guid.Empty, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        [Test]
        public async Task Handle_ShouldWork_WhenNoNotificationsToMarkAsRead()
        {
            // Arrange
            var command = new MarkAllAsReadCommand { UserId = _userId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                _notificationServiceMock.Verify(s => s.MarkAllAsReadAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        #endregion
    }
}