using Eduva.Application.Features.Notifications.Queries;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Constants;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Notifications.Queries
{
    [TestFixture]
    public class GetUserNotificationsQueryHandlerTests
    {
        #region Fields and Setup

        private Mock<INotificationService> _notificationServiceMock;
        private GetUserNotificationsQueryHandler _handler;
        private readonly Guid _userId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _handler = new GetUserNotificationsQueryHandler(_notificationServiceMock.Object);
        }

        #endregion

        #region Handle Tests

        [Test]
        public async Task Handle_ShouldReturnPaginatedResults_WhenValidRequest()
        {
            // Arrange
            var query = new GetUserNotificationsQuery
            {
                UserId = _userId,
                PageIndex = 2,
                PageSize = 10
            };

            var mockUserNotifications = new List<UserNotification>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsRead = true,
                    Notification = new Notification
                    {
                        Type = "Question",
                        Payload = """{"title":"Page 2 Question"}""",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }
            };

            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 10, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockUserNotifications);
            _notificationServiceMock.Setup(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(25);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(1));
                Assert.That(result.Count, Is.EqualTo(25));
                Assert.That(result.PageIndex, Is.EqualTo(2));
                Assert.That(result.PageSize, Is.EqualTo(10));
                Assert.That(result.Data.First().Type, Is.EqualTo("Question"));
            });

            Assert.Multiple(() =>
            {
                _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, 10, 10, It.IsAny<CancellationToken>()), Times.Once);
                _notificationServiceMock.Verify(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
            });
        }

        [Test]
        public async Task Handle_ShouldUseDefaultValues_WhenNoPageParametersProvided()
        {
            // Arrange
            var query = new GetUserNotificationsQuery { UserId = _userId };
            var expectedSkip = (AppConstants.DEFAULT_PAGE_INDEX - 1) * AppConstants.DEFAULT_PAGE_SIZE;

            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, expectedSkip, AppConstants.DEFAULT_PAGE_SIZE, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _notificationServiceMock.Setup(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.PageIndex, Is.EqualTo(AppConstants.DEFAULT_PAGE_INDEX));
                Assert.That(result.PageSize, Is.EqualTo(AppConstants.DEFAULT_PAGE_SIZE));
            });

            _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, expectedSkip, AppConstants.DEFAULT_PAGE_SIZE, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnEmptyResults_WhenNoNotifications()
        {
            // Arrange
            var query = new GetUserNotificationsQuery { UserId = _userId };

            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _notificationServiceMock.Setup(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Is.Empty);
                Assert.That(result.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task Handle_ShouldReturnAllNotifications_WhenPageSizeIsZero()
        {
            // Arrange
            var request = new GetUserNotificationsQuery { UserId = _userId, PageIndex = 1, PageSize = 0 };
            var notifications = new List<UserNotification> { /* ...mock data... */ };

            _notificationServiceMock
                .Setup(s => s.GetUserNotificationsAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result.Data, Has.Count.EqualTo(notifications.Count));
                Assert.That(result.Count, Is.EqualTo(notifications.Count));
            });
            _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Pagination Tests

        [Test]
        public async Task Handle_ShouldCalculateSkipCorrectly_WithDifferentPageValues()
        {
            // Arrange
            var query = new GetUserNotificationsQuery
            {
                UserId = _userId,
                PageIndex = 3,
                PageSize = 5
            };

            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, 10, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _notificationServiceMock.Setup(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert - verify skip calculation: (3-1) * 5 = 10
            _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, 10, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestCase(1, 10, 0)]
        [TestCase(2, 10, 10)]
        [TestCase(3, 5, 10)]
        [TestCase(5, 20, 80)]
        public async Task Handle_ShouldCalculateSkipCorrectly_ForVariousPageValues(int pageIndex, int pageSize, int expectedSkip)
        {
            // Arrange
            var query = new GetUserNotificationsQuery
            {
                UserId = _userId,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            _notificationServiceMock.Setup(s => s.GetUserNotificationsAsync(_userId, expectedSkip, pageSize, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            _notificationServiceMock.Setup(s => s.GetTotalCountAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(_userId, expectedSkip, pageSize, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}