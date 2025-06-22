using Eduva.API.Controllers.SchoolSubscriptions;
using Eduva.API.Models;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.SchoolSubscription
{
    [TestFixture]
    public class SchoolSubscriptionControllerTests
    {
        private Mock<IMediator> _mediatorMock = null!;
        private Mock<ILogger<SchoolSubscriptionController>> _loggerMock = null!;
        private SchoolSubscriptionController _controller = null!;

        #region SchoolSubscriptionControllerTests Setup

        [SetUp]
        public void SetUp()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<SchoolSubscriptionController>>();
            _controller = new SchoolSubscriptionController(_mediatorMock.Object, _loggerMock.Object);
        }

        #endregion

        #region CreatePaymentLink Tests

        [Test]
        public async Task CreatePaymentLink_ShouldReturn200_WithValidCommand()
        {
            // Arrange
            var command = new CreateSchoolSubscriptionCommand
            {
                PlanId = 1,
                SchoolId = 2,
                BillingCycle = BillingCycle.Yearly
            };

            var expectedResponse = new CreatePaymentLinkResponse
            {
                CheckoutUrl = "https://test.com",
                PaymentLinkId = "pay123",
                Amount = 123000
            };

            _mediatorMock
                .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync((CustomCode.Success, expectedResponse));

            // Act
            var result = await _controller.CreatePaymentLink(command);

            // Assert
            var okResult = result as ObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.Not.Null);
            });
        }

        [Test]
        public async Task CreatePaymentLink_ShouldReturn500_WhenExceptionThrown()
        {
            // Arrange
            var command = new CreateSchoolSubscriptionCommand
            {
                PlanId = 1,
                SchoolId = 2,
                BillingCycle = BillingCycle.Monthly
            };

            _mediatorMock
                .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Something failed"));

            // Act
            var result = await _controller.CreatePaymentLink(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region PayOSReturn Tests

        [Test]
        public async Task PayOSReturn_ShouldReturn200_WhenValid()
        {
            // Arrange
            var query = new ConfirmPayOSPaymentReturnCommand
            {
                Code = "00",
                Id = "xyz123",
                Status = "PAID",
                OrderCode = 111111
            };

            _mediatorMock
                .Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.PayOSReturn(query);

            // Assert
            var okResult = result as ObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task PayOSReturn_ShouldReturn500_WhenExceptionThrown()
        {
            // Arrange
            var query = new ConfirmPayOSPaymentReturnCommand
            {
                Code = "00",
                Id = "abc",
                Status = "PAID",
                OrderCode = 123456
            };

            _mediatorMock
                .Setup(x => x.Send(query, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Payment failed"));

            // Act
            var result = await _controller.PayOSReturn(query);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetCurrentSubscription Tests

        [Test]
        public async Task GetCurrentSubscription_ShouldReturn200_WhenValidUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            var expectedResponse = new MySchoolSubscriptionResponse
            {
                PlanName = "Plus",
                Description = "Advanced plan",
                StartDate = DateTimeOffset.UtcNow.AddDays(-5),
                EndDate = DateTimeOffset.UtcNow.AddDays(25),
                SubscriptionStatus = SubscriptionStatus.Active,
                BillingCycle = BillingCycle.Monthly,
                MaxUsers = 100,
                StorageLimitGB = 50,
                AmountPaid = 990000
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetMySchoolSubscriptionQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetCurrentSubscription();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(objectResult.Value);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<MySchoolSubscriptionResponse>>(json);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.Data!.PlanName, Is.EqualTo("Plus"));
        }

        [Test]
        public async Task GetCurrentSubscription_ShouldReturn401_WhenUnauthenticated()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            var result = await _controller.GetCurrentSubscription();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task GetCurrentSubscription_ShouldReturn500_WhenExceptionThrown()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetMySchoolSubscriptionQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Something failed"));

            // Act
            var result = await _controller.GetCurrentSubscription();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

    }
}