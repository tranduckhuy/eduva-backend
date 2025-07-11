using Eduva.Application.Features.Notifications.Queries;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Notifications.Queries
{
    [TestFixture]
    public class GetUnreadNotificationsQueryHandlerTests
    {
        #region Fields and Setup

        private Mock<INotificationService> _notificationServiceMock;
        private GetUnreadNotificationsQueryHandler _handler;
        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _handler = new GetUnreadNotificationsQueryHandler(_notificationServiceMock.Object);
        }

        #endregion

        #region Handle Tests

        [Test]
        public async Task Handle_ShouldReturnUnreadNotifications_WhenValidRequest()
        {
            // Arrange
            var query = new GetUnreadNotificationsQuery { UserId = _userId };
            var mockUserNotifications = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Type = "Question",
                        Payload = """{"title":"Unread Question"}""",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Type = "Comment",
                        Payload = """{"content":"Unread Comment"}""",
                        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserNotifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.First().Type, Is.EqualTo("Question"));
                Assert.That(result.First().IsRead, Is.False);
                Assert.That(result.Last().Type, Is.EqualTo("Comment"));
                Assert.That(result.Last().IsRead, Is.False);
            });

            _notificationServiceMock.Verify(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnEmptyList_WhenNoUnreadNotifications()
        {
            // Arrange
            var query = new GetUnreadNotificationsQuery { UserId = _userId };

            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Empty);
            });

            _notificationServiceMock.Verify(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public async Task Handle_ShouldHandleInvalidJson_Gracefully()
        {
            // Arrange
            var query = new GetUnreadNotificationsQuery { UserId = _userId };
            var invalidJsonNotification = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Type = "Invalid",
                        Payload = """{"invalid": json}""", // Invalid JSON
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidJsonNotification);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.First().Payload, Is.Not.Null); // Should handle invalid JSON gracefully
            });
        }

        [Test]
        public async Task Handle_ShouldHandleNullPayload_Gracefully()
        {
            // Arrange
            var query = new GetUnreadNotificationsQuery { UserId = _userId };
            var nullPayloadNotification = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Type = "Null",
                        Payload = null!, // Null payload
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUnreadNotificationsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(nullPayloadNotification);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.First().Payload, Is.Not.Null);
            });
        }

        #endregion
    }
}