using Eduva.Application.Features.Notifications.Queries;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Notifications.Queries
{
    [TestFixture]
    public class GetNotificationSummaryQueryHandlerTests
    {
        #region Fields and Setup

        private Mock<INotificationService> _notificationServiceMock;
        private GetNotificationSummaryQueryHandler _handler;
        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _handler = new GetNotificationSummaryQueryHandler(_notificationServiceMock.Object);
        }

        #endregion

        #region Handle Tests

        [Test]
        public async Task Handle_ShouldReturnCorrectSummary_WhenValidRequest()
        {
            // Arrange
            var query = new GetNotificationSummaryQuery { UserId = _userId };
            var mockUserNotifications = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Type = "Question",
                        Payload = """{"title":"Test Question"}""",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = true,
                    Notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Type = "Comment",
                        Payload = """{"content":"Test Comment"}""",
                        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);
            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 0, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserNotifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.UnreadCount, Is.EqualTo(5));
                Assert.That(result.RecentNotifications, Has.Count.EqualTo(2));
                Assert.That(result.RecentNotifications.First().Type, Is.EqualTo("Question"));
                Assert.That(result.RecentNotifications.First().IsRead, Is.False);
                Assert.That(result.RecentNotifications.Last().Type, Is.EqualTo("Comment"));
                Assert.That(result.RecentNotifications.Last().IsRead, Is.True);
            });

            Assert.Multiple(() =>
            {
                _notificationServiceMock.Verify(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
                _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, 0, 5, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        [Test]
        public async Task Handle_ShouldReturnEmptyRecentNotifications_WhenNoNotifications()
        {
            // Arrange
            var query = new GetNotificationSummaryQuery { UserId = _userId };

            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 0, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.UnreadCount, Is.EqualTo(0));
                Assert.That(result.RecentNotifications, Is.Empty);
            });
        }

        [Test]
        public async Task Handle_ShouldHandleEmptyPayload_Gracefully()
        {
            // Arrange
            var query = new GetNotificationSummaryQuery { UserId = _userId };
            var mockUserNotifications = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = false,
                    Notification = new Notification
                    {
                        Type = "Empty",
                        Payload = "", // Empty payload
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 0, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserNotifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.RecentNotifications.First().Payload, Is.Not.Null);
            });
        }

        #endregion

        #region Edge Cases Tests

        [Test]
        public async Task Handle_ShouldHandleInvalidJson_Gracefully()
        {
            // Arrange
            var query = new GetNotificationSummaryQuery { UserId = _userId };
            var mockUserNotifications = new List<UserNotification>
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

            _notificationServiceMock.Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 0, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserNotifications);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.RecentNotifications.First().Payload, Is.Not.Null);
                Assert.That(result.RecentNotifications.First().Type, Is.EqualTo("Invalid"));
            });
        }

        #endregion
    }
}