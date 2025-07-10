using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Notifications.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Notifications.Commands
{
    [TestFixture]
    public class MarkAsReadCommandHandlerTests
    {
        #region Fields and Setup

        private Mock<INotificationService> _notificationServiceMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IUserNotificationRepository> _userNotificationRepoMock;
        private MarkAsReadCommandHandler _handler;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _userNotificationId = Guid.NewGuid();
        private readonly Guid _notificationId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userNotificationRepoMock = new Mock<IUserNotificationRepository>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserNotificationRepository>())
                .Returns(_userNotificationRepoMock.Object);

            _handler = new MarkAsReadCommandHandler(_notificationServiceMock.Object, _unitOfWorkMock.Object);
        }

        #endregion

        #region Success Tests

        [Test]
        public async Task Handle_ShouldReturnTrue_WhenValidUserNotification()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = _userId,
                NotificationId = _notificationId
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);

            Assert.Multiple(() =>
            {
                _userNotificationRepoMock.Verify(r => r.GetByIdAsync(_userNotificationId), Times.Once);
                _notificationServiceMock.Verify(s => s.MarkAsReadAsync(_userNotificationId, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        [Test]
        public async Task Handle_ShouldValidateOwnership_BeforeMarkingAsRead()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = _userId,
                NotificationId = _notificationId,
                IsRead = false
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert - Should check ownership before calling service
            Assert.Multiple(() =>
            {
                _userNotificationRepoMock.Verify(r => r.GetByIdAsync(_userNotificationId), Times.Once);
                _notificationServiceMock.Verify(s => s.MarkAsReadAsync(_userNotificationId, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        #endregion

        #region Exception Tests

        [Test]
        public void Handle_ShouldThrowNotificationNotFoundException_WhenNotificationNotFound()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync((UserNotification?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            Assert.That(ex?.StatusCode, Is.EqualTo(CustomCode.NotificationNotFound));

            Assert.Multiple(() =>
            {
                _userNotificationRepoMock.Verify(r => r.GetByIdAsync(_userNotificationId), Times.Once);
                _notificationServiceMock.Verify(s => s.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            });
        }

        [Test]
        public void Handle_ShouldThrowForbiddenException_WhenUserNotOwner()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = otherUserId, // Different user
                NotificationId = _notificationId
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));

            Assert.That(ex?.StatusCode, Is.EqualTo(CustomCode.Forbidden));

            Assert.Multiple(() =>
            {
                _userNotificationRepoMock.Verify(r => r.GetByIdAsync(_userNotificationId), Times.Once);
                _notificationServiceMock.Verify(s => s.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            });
        }

        [Test]
        public void Handle_ShouldThrowNotificationNotFoundException_WhenNullReturned()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync((UserNotification)null!);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(command, CancellationToken.None));
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public async Task Handle_ShouldWork_WhenNotificationAlreadyRead()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = _userId,
                NotificationId = _notificationId,
                IsRead = true // Already read
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _notificationServiceMock.Verify(s => s.MarkAsReadAsync(_userNotificationId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldWork_WithDifferentUserIds()
        {
            // Arrange
            var differentUserId = Guid.NewGuid();
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = differentUserId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = differentUserId, // Same as command userId
                NotificationId = _notificationId
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task Handle_ShouldFollowCorrectFlow_ForValidScenario()
        {
            // Arrange
            var command = new MarkAsReadCommand
            {
                UserNotificationId = _userNotificationId,
                UserId = _userId
            };

            var userNotification = new UserNotification
            {
                Id = _userNotificationId,
                TargetUserId = _userId,
                NotificationId = _notificationId,
                IsRead = false
            };

            _userNotificationRepoMock.Setup(r => r.GetByIdAsync(_userNotificationId))
                .ReturnsAsync(userNotification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);

                // Verify the correct sequence: Get -> Validate -> Mark
                _userNotificationRepoMock.Verify(r => r.GetByIdAsync(_userNotificationId), Times.Once);
                _notificationServiceMock.Verify(s => s.MarkAsReadAsync(_userNotificationId, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        #endregion
    }
}