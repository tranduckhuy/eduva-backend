using Eduva.API.Controllers.CreditTransactions;
using Eduva.API.Models;
using Eduva.Application.Features.CreditTransactions.Commands;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.CreditTransactions
{
    [TestFixture]
    public class CreditTransactionControllerTests
    {
        private CreditTransactionController _controller = default!;
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<CreditTransactionController>> _loggerMock = default!;

        #region CreditTransactionControllerTest Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<CreditTransactionController>>();
            _controller = new CreditTransactionController(_mediatorMock.Object, _loggerMock.Object);

            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #endregion

        #region CreatePaymentLink Test

        [Test]
        public async Task CreatePaymentLink_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var command = new CreateCreditPackPaymentLinkCommand { CreditPackId = 1 };
            var response = new CreateCreditPackPaymentLinkResponse
            {
                CheckoutUrl = "https://payos.test/checkout",
                PaymentLinkId = "sdsdsd",
                Amount = 50000,
                TransactionCode = "1687500000"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateCreditPackPaymentLinkCommand>(), default))
                .ReturnsAsync((CustomCode.Success, response));

            // Act
            var result = await _controller.CreatePaymentLink(command);

            // Assert
            var okResult = result as ObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var data = okResult.Value as ApiResponse<object>;
            Assert.That(data, Is.Not.Null);
            Assert.That(data!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task CreatePaymentLink_ShouldReturnUserIdNotFound_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // no user
            };

            var command = new CreateCreditPackPaymentLinkCommand();

            // Act
            var result = await _controller.CreatePaymentLink(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        #endregion

    }
}