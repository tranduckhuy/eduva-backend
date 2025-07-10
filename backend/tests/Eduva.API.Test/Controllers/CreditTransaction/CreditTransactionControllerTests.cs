using Eduva.API.Controllers.CreditTransactions;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.CreditTransactions.Commands;
using Eduva.Application.Features.CreditTransactions.Queries;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Features.CreditTransactions.Specifications;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.CreditTransaction
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

        #region GetUserCreditTransactions Tests

        [Test]
        public async Task GetUserCreditTransactions_ShouldReturnOk_WhenSystemAdminRequestsAllTransactions()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, nameof(Role.SystemAdmin))
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var specParam = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                UserId = Guid.NewGuid() // SystemAdmin can specify any UserId
            };

            var originalUserId = specParam.UserId;

            var expectedResponse = new Pagination<CreditTransactionResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<CreditTransactionResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Credits = 100,
                        CreatedAt = DateTimeOffset.UtcNow,
                        User = new UserInfo { Id = Guid.NewGuid(), FullName = "Test User" },
                        AICreditPack = new AICreditPackInfor { Id = 1, Name = "Starter Pack", Price = 50000, Credits = 100, BonusCredits = 10 },
                        PaymentTransactionId = Guid.NewGuid()
                    }
                }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetCreditTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUserCreditTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            // Verify that specParam.UserId was NOT overridden for SystemAdmin
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetCreditTransactionQuery>(q => q.Param.UserId == originalUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserCreditTransactions_ShouldOverrideUserId_WhenTeacherMakesRequest()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                new(ClaimTypes.Role, nameof(Role.Teacher))
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var specParam = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                UserId = Guid.NewGuid() // Teacher tries to specify different UserId
            };

            var expectedResponse = new Pagination<CreditTransactionResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<CreditTransactionResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Credits = 100,
                        CreatedAt = DateTimeOffset.UtcNow,
                        User = new UserInfo { Id = currentUserId, FullName = "Teacher User" },
                        AICreditPack = new AICreditPackInfor { Id = 1, Name = "Starter Pack", Price = 50000, Credits = 100, BonusCredits = 10 },
                        PaymentTransactionId = Guid.NewGuid()
                    }
                }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetCreditTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUserCreditTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            // Verify that specParam.UserId was overridden to current user's ID
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetCreditTransactionQuery>(q => q.Param.UserId == currentUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserCreditTransactions_ShouldOverrideUserId_WhenContentModeratorMakesRequest()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                new(ClaimTypes.Role, nameof(Role.ContentModerator))
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var specParam = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                UserId = Guid.NewGuid() // ContentModerator tries to specify different UserId
            };

            var expectedResponse = new Pagination<CreditTransactionResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<CreditTransactionResponse>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Credits = 100,
                        CreatedAt = DateTimeOffset.UtcNow,
                        User = new UserInfo { Id = currentUserId, FullName = "ContentModerator User" },
                        AICreditPack = new AICreditPackInfor { Id = 1, Name = "Starter Pack", Price = 50000, Credits = 100, BonusCredits = 10 },
                        PaymentTransactionId = Guid.NewGuid()
                    }
                }
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetCreditTransactionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUserCreditTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            // Verify that specParam.UserId was overridden to current user's ID
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetCreditTransactionQuery>(q => q.Param.UserId == currentUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserCreditTransactions_ShouldReturnUserIdNotFound_WhenNonSystemAdminUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // no user
            };

            var specParam = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetUserCreditTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            // Verify that mediator was never called
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetCreditTransactionQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GetUserCreditTransactions_ShouldReturnUserIdNotFound_WhenTeacherHasInvalidUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "invalid-guid"),
                new(ClaimTypes.Role, nameof(Role.Teacher))
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var specParam = new CreditTransactionSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetUserCreditTransactions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            // Verify that mediator was never called
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetCreditTransactionQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region GetCreditTransactionById Tests

        [Test]
        public async Task GetCreditTransactionById_ShouldReturnOk_WhenTransactionExists()
        {
            // Arrange
            var transactionId = Guid.NewGuid();
            var expectedResponse = new CreditTransactionResponse
            {
                Id = transactionId,
                Credits = 300,
                CreatedAt = DateTimeOffset.UtcNow,
                User = new UserInfo
                {
                    Id = Guid.NewGuid(),
                    FullName = "Jane Doe",
                    Email = "jane@example.com"
                },
                AICreditPack = new AICreditPackInfor
                {
                    Id = 2,
                    Name = "Pro Pack",
                    Price = 250000,
                    Credits = 300,
                    BonusCredits = 50
                },
                PaymentTransactionId = Guid.NewGuid()
            };

            _mediatorMock
                 .Setup(m => m.Send(It.IsAny<GetCreditTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetCreditTransactionById(transactionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            // Verify that the query was called with correct parameters
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetCreditTransactionByIdQuery>(q =>
                    q.Id == transactionId &&
                    q.IsSystemAdmin == false), // Default user is not admin
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetCreditTransactionById_ShouldReturnUserIdNotFound_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // no user
            };

            var transactionId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCreditTransactionById(transactionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(401));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetCreditTransactionById_ShouldPassSystemAdminFlag_WhenUserIsSystemAdmin()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Role, nameof(Role.SystemAdmin))
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var transactionId = Guid.NewGuid();
            var expectedResponse = new CreditTransactionResponse { Id = transactionId };

            _mediatorMock
                 .Setup(m => m.Send(It.IsAny<GetCreditTransactionByIdQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetCreditTransactionById(transactionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            // Verify that the query was called with SystemAdmin = true
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetCreditTransactionByIdQuery>(q =>
                    q.Id == transactionId &&
                    q.IsSystemAdmin == true &&
                    q.UserId == Guid.Parse(userId)),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}