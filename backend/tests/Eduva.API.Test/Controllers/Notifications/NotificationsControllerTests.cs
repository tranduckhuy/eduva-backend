using Eduva.API.Controllers.Notifications;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Notifications.Commands;
using Eduva.Application.Features.Notifications.Queries;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Domain.Constants;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Notifications
{
    [TestFixture]
    public class NotificationsControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<NotificationsController>> _loggerMock;
        private NotificationsController _controller;
        private readonly Guid _validUserId = Guid.NewGuid();
        private readonly Guid _notificationId = Guid.NewGuid();

        #region Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<NotificationsController>>();
            _controller = new NotificationsController(_loggerMock.Object, _mediatorMock.Object);
        }

        private void SetupUser(string? userId = null)
        {
            var userIdValue = userId ?? _validUserId.ToString();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userIdValue) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        #endregion

        #region GetNotifications Tests

        [TestCase(null, null, AppConstants.DEFAULT_PAGE_INDEX, AppConstants.DEFAULT_PAGE_SIZE)]
        [TestCase(2, 5, 2, 5)]
        [TestCase(1, null, 1, AppConstants.DEFAULT_PAGE_SIZE)]
        [TestCase(null, 10, AppConstants.DEFAULT_PAGE_INDEX, 10)]
        public async Task GetNotifications_ShouldReturn200_WithPagination(int? pageIndex, int? pageSize, int expectedPageIndex, int expectedPageSize)
        {
            // Arrange
            SetupUser();
            var expectedResponse = new Pagination<NotificationResponse>
            {
                Data = new List<NotificationResponse> { new() { Id = Guid.NewGuid() } },
                Count = 1,
                PageIndex = expectedPageIndex,
                PageSize = expectedPageSize
            };

            _mediatorMock.Setup(m => m.Send(It.Is<GetUserNotificationsQuery>(q =>
                q.UserId == _validUserId && q.PageIndex == expectedPageIndex && q.PageSize == expectedPageSize),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetNotifications(pageIndex, pageSize);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetNotifications_ShouldReturn400_WhenUserIdInvalid()
        {
            // Arrange
            SetupUser("invalid-guid");

            // Act
            var result = await _controller.GetNotifications(null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
                var response = objectResult?.Value as ApiResponse<object>;
                Assert.That(response?.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
            });
        }

        [TestCase(typeof(AppException), CustomCode.NotificationNotFound, StatusCodes.Status404NotFound)]
        [TestCase(typeof(Exception), CustomCode.SystemError, StatusCodes.Status500InternalServerError)]
        public async Task GetNotifications_ShouldHandleExceptions(Type exceptionType, CustomCode expectedCode, int expectedStatusCode)
        {
            // Arrange
            SetupUser();
            var exception = exceptionType == typeof(AppException)
                ? new AppException(expectedCode)
                : new Exception("Test error");

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetNotifications(null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult?.StatusCode, Is.EqualTo(expectedStatusCode));
                var response = objectResult?.Value as ApiResponse<object>;
                Assert.That(response?.StatusCode, Is.EqualTo((int)expectedCode));
            });
        }

        #endregion

        #region GetUnreadNotifications Tests

        [Test]
        public async Task GetUnreadNotifications_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            SetupUser();
            var expectedResponse = new List<NotificationResponse>
            {
                new() { Id = Guid.NewGuid(), IsRead = false }
            };

            _mediatorMock.Setup(m => m.Send(It.Is<GetUnreadNotificationsQuery>(q => q.UserId == _validUserId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUnreadNotifications();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetUnreadNotifications_ShouldReturn400_WhenUserIdInvalid()
        {
            // Arrange
            SetupUser("invalid");

            // Act
            var result = await _controller.GetUnreadNotifications();

            // Assert
            AssertUserIdNotFound(result);
        }

        #endregion

        #region GetNotificationSummary Tests

        [Test]
        public async Task GetNotificationSummary_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            SetupUser();
            var expectedResponse = new NotificationSummaryResponse
            {
                UnreadCount = 3,
                RecentNotifications = new List<NotificationResponse>()
            };

            _mediatorMock.Setup(m => m.Send(It.Is<GetNotificationSummaryQuery>(q => q.UserId == _validUserId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetNotificationSummary();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetNotificationSummary_ShouldReturn400_WhenUserIdInvalid()
        {
            // Arrange
            SetupUser("invalid");

            // Act
            var result = await _controller.GetNotificationSummary();

            // Assert
            AssertUserIdNotFound(result);
        }

        #endregion

        #region MarkAsRead Tests

        [Test]
        public async Task MarkAsRead_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.Is<MarkAsReadCommand>(c =>
                c.UserNotificationId == _notificationId && c.UserId == _validUserId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.MarkAsRead(_notificationId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task MarkAsRead_ShouldReturn400_WhenUserIdInvalid()
        {
            // Arrange
            SetupUser("invalid");

            // Act
            var result = await _controller.MarkAsRead(_notificationId);

            // Assert
            AssertUserIdNotFound(result);
        }

        [TestCase(CustomCode.NotificationNotFound, StatusCodes.Status404NotFound)]
        [TestCase(CustomCode.Forbidden, StatusCodes.Status403Forbidden)]
        public async Task MarkAsRead_ShouldHandleAppExceptions(CustomCode errorCode, int expectedStatusCode)
        {
            // Arrange
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAsReadCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AppException(errorCode));

            // Act
            var result = await _controller.MarkAsRead(_notificationId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult?.StatusCode, Is.EqualTo(expectedStatusCode));
                var response = objectResult?.Value as ApiResponse<object>;
                Assert.That(response?.StatusCode, Is.EqualTo((int)errorCode));
            });
        }

        #endregion

        #region MarkAllAsRead Tests

        [Test]
        public async Task MarkAllAsRead_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.Is<MarkAllAsReadCommand>(c => c.UserId == _validUserId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task MarkAllAsRead_ShouldReturn400_WhenUserIdInvalid()
        {
            // Arrange
            SetupUser("invalid");

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            AssertUserIdNotFound(result);
        }

        [Test]
        public async Task MarkAllAsRead_ShouldReturn500_WhenExceptionThrown()
        {
            // Arrange
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAllAsReadCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
                var response = objectResult?.Value as ApiResponse<object>;
                Assert.That(response?.StatusCode, Is.EqualTo((int)CustomCode.SystemError));
            });
        }

        #endregion

        #region Helper Methods

        private static void AssertUserIdNotFound(IActionResult result)
        {
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult?.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
                var response = objectResult?.Value as ApiResponse<object>;
                Assert.That(response?.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
            });
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task AllMethods_ShouldCallMediator_WhenValidInput()
        {
            // Arrange
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<NotificationResponse>());
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUnreadNotificationsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<NotificationResponse>());
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetNotificationSummaryQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NotificationSummaryResponse());
            _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAsReadCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAllAsReadCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _controller.GetNotifications(null, null);
            await _controller.GetUnreadNotifications();
            await _controller.GetNotificationSummary();
            await _controller.MarkAsRead(_notificationId);
            await _controller.MarkAllAsRead();

            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetUnreadNotificationsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetNotificationSummaryQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(m => m.Send(It.IsAny<MarkAsReadCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(m => m.Send(It.IsAny<MarkAllAsReadCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}