using Eduva.API.Attributes;
using Eduva.API.Middlewares;
using Eduva.API.Models;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Eduva.API.Test.Middlewares
{
    [TestFixture]
    public class SubscriptionValidationMiddlewareTests
    {
        private Mock<RequestDelegate> _nextMock;
        private Mock<ISchoolSubscriptionService> _subscriptionServiceMock;
        private SubscriptionValidationMiddleware _middleware;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void Setup()
        {
            _nextMock = new Mock<RequestDelegate>();
            _subscriptionServiceMock = new Mock<ISchoolSubscriptionService>();
            _middleware = new SubscriptionValidationMiddleware(_nextMock.Object);
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
        }

        #region No Access Required Tests

        [Test]
        public async Task Invoke_ShouldCallNext_WhenNoSubscriptionAccessRequired()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.None);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(_httpContext), Times.Once);
        }

        #endregion

        #region Authentication Tests

        [Test]
        public async Task Invoke_ShouldReturn401_WhenUserIdIsNull()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser(null, new[] { nameof(Role.Teacher) });

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(401));
            await AssertResponseMessage("Unauthorized. User ID or roles not found.");
        }

        [Test]
        public async Task Invoke_ShouldReturn401_WhenNoRoles()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", Array.Empty<string>());

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(401));
            await AssertResponseMessage("Unauthorized. User ID or roles not found.");
        }

        #endregion

        #region SystemAdmin Bypass Tests

        [Test]
        public async Task Invoke_ShouldCallNext_WhenUserIsSystemAdmin()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadWrite);
            SetupUser("123", new[] { nameof(Role.SystemAdmin) });

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(_httpContext), Times.Once);
        }

        #endregion

        #region SchoolAdmin School Setup Tests

        [Test]
        public async Task Invoke_ShouldReturn403_WhenSchoolAdminWithoutSchoolAccessingNonSchoolEndpoint()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.SchoolAdmin) }, schoolId: null);
            _httpContext.Request.Path = "/api/classes";

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(403));
            await AssertResponseMessage("Forbidden. You must complete school and subscription information to access this resource.");
        }

        [Test]
        public async Task Invoke_ShouldContinue_WhenSchoolAdminWithoutSchoolAccessingSchoolsEndpoint()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.SchoolAdmin) }, schoolId: null);
            _httpContext.Request.Path = "/api/schools/create";

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.Not.EqualTo(403));
        }

        #endregion

        #region School Validation Tests

        [Test]
        public async Task Invoke_ShouldReturn404_WhenSchoolIdIsEmpty()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "");

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(404));
            await AssertResponseMessage("School not found or invalid school ID.");
        }

        [Test]
        public async Task Invoke_ShouldReturn404_WhenSchoolIdIsNotValidInteger()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "invalid");

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(404));
            await AssertResponseMessage("School not found or invalid school ID.");
        }

        #endregion

        #region Subscription Tests

        [Test]
        public async Task Invoke_ShouldReturn404_WhenSubscriptionNotFound()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "1");
            _subscriptionServiceMock.Setup(s => s.GetCurrentSubscriptionAsync(1))
                .ReturnsAsync((SchoolSubscription?)null);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(404));
            await AssertResponseMessage("School subscription not found.");
        }

        [Test]
        public async Task Invoke_ShouldCallNext_WhenSubscriptionIsActive()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadWrite);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "1");
            var activeSubscription = CreateSubscription(DateTimeOffset.UtcNow.AddDays(30));
            _subscriptionServiceMock.Setup(s => s.GetCurrentSubscriptionAsync(1))
                .ReturnsAsync(activeSubscription);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(_httpContext), Times.Once);
        }

        [Test]
        public async Task Invoke_ShouldReturn402_WhenSubscriptionExpiredAndNotInGracePeriod()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadWrite);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "1");
            var expiredSubscription = CreateSubscription(DateTimeOffset.UtcNow.AddDays(-20));
            _subscriptionServiceMock.Setup(s => s.GetCurrentSubscriptionAsync(1))
                .ReturnsAsync(expiredSubscription);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(402));
            await AssertResponseMessage("School subscription has expired. Access denied. Please renew your subscription.");
        }

        [Test]
        public async Task Invoke_ShouldReturn402_WhenSubscriptionExpiredAndReadWriteAccessInGracePeriod()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadWrite);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "1");
            var expiredSubscription = CreateSubscription(DateTimeOffset.UtcNow.AddDays(-5)); // Within 14-day grace period
            _subscriptionServiceMock.Setup(s => s.GetCurrentSubscriptionAsync(1))
                .ReturnsAsync(expiredSubscription);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo(402));
            await AssertResponseMessage("School subscription has expired. Access denied. Please renew your subscription.");
        }

        [Test]
        public async Task Invoke_ShouldCallNext_WhenSubscriptionExpiredButReadOnlyAccessInGracePeriod()
        {
            // Arrange
            SetupEndpoint(SubscriptionAccessLevel.ReadOnly);
            SetupUser("123", new[] { nameof(Role.Teacher) }, schoolId: "1");
            var expiredSubscription = CreateSubscription(DateTimeOffset.UtcNow.AddDays(-5)); // Within 14-day grace period
            _subscriptionServiceMock.Setup(s => s.GetCurrentSubscriptionAsync(1))
                .ReturnsAsync(expiredSubscription);

            // Act
            await _middleware.Invoke(_httpContext, _subscriptionServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(_httpContext), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupEndpoint(SubscriptionAccessLevel accessLevel)
        {
            var endpoint = new Endpoint(c => Task.CompletedTask,
                new EndpointMetadataCollection(new SubscriptionAccessAttribute(accessLevel)),
                "TestEndpoint");
            _httpContext.SetEndpoint(endpoint);
        }

        private void SetupUser(string? userId, string[] roles, string? schoolId = "1")
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (!string.IsNullOrEmpty(schoolId))
                claims.Add(new Claim("SchoolId", schoolId));

            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        private static SchoolSubscription CreateSubscription(DateTimeOffset endDate)
        {
            return new SchoolSubscription
            {
                Id = 1,
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = endDate,
                SubscriptionStatus = SubscriptionStatus.Active,
                SchoolId = 1
            };
        }

        private async Task AssertResponseMessage(string expectedMessage)
        {
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
            var response = JsonConvert.DeserializeObject<ApiResponse<object>>(responseBody);
            Assert.That(response?.Message, Is.EqualTo(expectedMessage));
        }

        #endregion
    }
}
