using Eduva.API.Controllers.Payments;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Payments.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Payment
{
    [TestFixture]
    public class PaymentControllerTests
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<PaymentController>> _loggerMock = default!;
        private PaymentController _controller = default!;

        #region PaymentControllerTests Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<PaymentController>>();
            _controller = new PaymentController(_mediatorMock.Object, _loggerMock.Object);
        }

        #endregion

        #region PayOSReturn Tests

        [Test]
        public async Task PayOSReturn_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var query = new ConfirmPayOSPaymentReturnCommand
            {
                Code = "00",
                Id = "some-id",
                Status = "PAID",
                OrderCode = 123456
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ConfirmPayOSPaymentReturnCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.PayOSReturn(query);

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task PayOSReturn_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var query = new ConfirmPayOSPaymentReturnCommand
            {
                Code = "01",
                Id = "invalid-id",
                Status = "FAILED",
                OrderCode = 999999
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ConfirmPayOSPaymentReturnCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.PayOSReturn(query);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetPaymentTransactions Tests

        [Test]
        public async Task GetPaymentTransactions_ShouldReturnSuccess_WhenRequestIsValid()
        {
            // Arrange
            var specParam = new PaymentSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            var expected = new Pagination<PaymentResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<PaymentResponse>
        {
            new()
            {
                TransactionCode = "TRX001",
                Amount = 500000,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentPurpose = PaymentPurpose.SchoolSubscription,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTimeOffset.UtcNow,
                User = new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Nguyen Van A",
                    Email = "a@email.com",
                    PhoneNumber = "0123456789"
                }
            }
        }
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetPaymentTransactionsQuery>(q => q.Param == specParam), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetPaymentTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);

            var data = response!.Data as Pagination<PaymentResponse>;
            Assert.That(data, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(data!.Data, Has.Count.EqualTo(1));
                Assert.That(data.Data.First().TransactionCode, Is.EqualTo("TRX001"));
            });
        }

        #endregion

        #region GetMyPayments Tests

        [Test]
        public async Task GetMyPayments_ShouldReturnUserIdNotFound_WhenUserIdClaimIsMissing()
        {
            // Arrange
            var specParam = new MyPaymentSpecParam();
            var controller = SetupControllerWithUser(null);

            // Act
            var result = await controller.GetMyPayments(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetMyPayments_ShouldReturnUserIdNotFound_WhenUserIdIsInvalidGuid()
        {
            // Arrange
            var specParam = new MyPaymentSpecParam();
            var controller = SetupControllerWithUser("invalid-guid");

            // Act
            var result = await controller.GetMyPayments(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));
            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetMyPayments_ShouldReturnSuccess_WhenUserIdIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var specParam = new MyPaymentSpecParam();
            var controller = SetupControllerWithUser(userId.ToString());

            var expected = new Pagination<PaymentResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<PaymentResponse>
        {
            new PaymentResponse
            {
                TransactionCode = "PAY123",
                Amount = 200000,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTimeOffset.UtcNow,
                User = new()
                {
                    Id = userId,
                    FullName = "User",
                    Email = "user@example.com",
                    PhoneNumber = "123456789"
                }
            }
        }
            };

            _mediatorMock.Setup(m => m.Send(
                It.Is<GetMyPaymentsQuery>(q => q.SpecParam.UserId == userId),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // Act
            var result = await controller.GetMyPayments(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            var data = response.Data as Pagination<PaymentResponse>;
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.Data.First().TransactionCode, Is.EqualTo("PAY123"));
        }

        #endregion

        #region Methods Helpers

        private PaymentController SetupControllerWithUser(string? userId)
        {
            var controller = new PaymentController(_mediatorMock.Object, _loggerMock.Object);

            var claims = new List<Claim>();
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var identity = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            return controller;
        }

        #endregion

    }
}