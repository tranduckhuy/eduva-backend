using Eduva.API.Controllers.Payments;
using Eduva.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.API.Test.Controllers.Payments
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

    }
}