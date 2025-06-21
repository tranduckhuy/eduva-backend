using Eduva.API.Controllers.SubscriptionPlans;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.SubscriptionPlans.Commands.ActivatePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.ArchivePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.DeletePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using Eduva.Application.Features.SubscriptionPlans.Queries;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.SubscriptionPlans.Specifications;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.SubscriptionPlan
{
    [TestFixture]
    public class SubscriptionPlanControllerTests
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<SubscriptionPlanController>> _loggerMock = default!;
        private SubscriptionPlanController _controller = default!;

        #region SubscriptionPlanControllerTests Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<SubscriptionPlanController>>();
            _controller = new SubscriptionPlanController(_mediatorMock.Object, _loggerMock.Object);
            SetupUserWithRole("SystemAdmin");
        }

        #endregion

        #region GetSubscriptionPlans Tests

        [Test]
        public async Task GetSubscriptionPlans_ShouldReturnSuccess_WhenValidRequest()
        {
            var specParam = new SubscriptionPlanSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            var pagination = new Pagination<SubscriptionPlanResponse>(1, 10, 1, new List<SubscriptionPlanResponse>
            {
                new()
                {
                    Id = 1,
                    Name = "Standard",
                    MaxUsers = 5,
                    PriceMonthly = 100000,
                    PricePerYear = 1000000,
                    StorageLimitGB = 10,
                    MaxMinutesPerMonth = 500,
                    Status = Domain.Enums.EntityStatus.Active
                }
            });

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetSubscriptionPlansQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagination);

            var result = await _controller.GetSubscriptionPlans(specParam);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null, "ObjectResult is null");

            dynamic? value = objectResult!.Value;
            Assert.That(value, Is.Not.Null, "Response value is null");

            var statusCode = (int)value.GetType().GetProperty("StatusCode")?.GetValue(value, null)!;
            Assert.That(statusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task GetSubscriptionPlans_ShouldReturn500_WhenExceptionThrown()
        {
            var specParam = new SubscriptionPlanSpecParam();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionPlansQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.GetSubscriptionPlans(specParam);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetSubscriptionPlanById Tests

        [Test]
        public async Task GetSubscriptionPlanById_ShouldReturnSuccess()
        {
            var response = new SubscriptionPlanResponse { Id = 1, Name = "Plus" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSubscriptionPlanByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var result = await _controller.GetSubscriptionPlanById(1);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        #endregion

        #region GetSubscriptionPlanDetail Tests

        [Test]
        public async Task GetSubscriptionPlanDetail_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetSubscriptionPlanDetailQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetSubscriptionPlanDetail(1);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task GetSubscriptionPlanDetail_ShouldReturn200_WhenSuccess()
        {
            // Arrange
            var expectedResponse = new SubscriptionPlanDetailResponse
            {
                Id = 99,
                Name = "Ultimate Plan",
                MaxUsers = 100,
                PriceMonthly = 500000,
                PricePerYear = 5000000,
                StorageLimitGB = 500,
                MaxMinutesPerMonth = 5000,
                Status = Domain.Enums.EntityStatus.Active,
                Description = "Advanced plan with full features"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetSubscriptionPlanDetailQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetSubscriptionPlanDetail(99);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));

            dynamic? value = objectResult.Value;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.StatusCode, Is.EqualTo((int)CustomCode.Success));

            var plan = value.Data as SubscriptionPlanDetailResponse;
            Assert.That(plan, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(plan!.Id, Is.EqualTo(expectedResponse.Id));
                Assert.That(plan.Name, Is.EqualTo(expectedResponse.Name));
            });
        }

        #endregion

        #region CreateSubscriptionPlan Tests

        [Test]
        public async Task CreateSubscriptionPlan_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateSubscriptionPlanCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.CreateSubscriptionPlan(new CreateSubscriptionPlanCommand());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task CreateSubscriptionPlan_ShouldReturn200_WhenSuccess()
        {
            var command = new CreateSubscriptionPlanCommand { Name = "Basic" };

            _mediatorMock
                .Setup(m => m.Send<SubscriptionPlanResponse>(
                    It.IsAny<GetSubscriptionPlanByIdQuery>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(new SubscriptionPlanResponse { Id = 1, Name = "Basic" });



            var result = await _controller.CreateSubscriptionPlan(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
                Assert.That(objectResult.Value, Is.Not.Null);
            });
        }

        #endregion

        #region UpdateSubscriptionPlan Tests

        [Test]
        public async Task UpdateSubscriptionPlan_ShouldReturn200_WhenSuccess()
        {
            var command = new UpdateSubscriptionPlanCommand { Name = "Updated" };
            var id = 2;
            command.Id = id;

            _mediatorMock
               .Setup(m => m.Send<SubscriptionPlanResponse>(
                   It.IsAny<GetSubscriptionPlanByIdQuery>(),
                   It.IsAny<CancellationToken>()
               ))
               .ReturnsAsync(new SubscriptionPlanResponse { Id = 1, Name = "Basic" });

            var result = await _controller.UpdateSubscriptionPlan(id, command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
                Assert.That(objectResult.Value, Is.Not.Null);
            });
        }

        [Test]
        public async Task UpdateSubscriptionPlan_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateSubscriptionPlanCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.UpdateSubscriptionPlan(1, new UpdateSubscriptionPlanCommand());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region ActivateSubscriptionPlan Tests

        [Test]
        public async Task ActivateSubscriptionPlan_ShouldReturn200_WhenSuccess()
        {
            var command = new ActivateSubscriptionPlanCommand(1);

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ActivateSubscriptionPlan(1);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task ActivateSubscriptionPlan_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ActivateSubscriptionPlanCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.ActivateSubscriptionPlan(1);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region ArchiveSubscriptionPlan Tests

        [Test]
        public async Task ArchiveSubscriptionPlan_ShouldReturn200_WhenSuccess()
        {
            var command = new ArchiveSubscriptionPlanCommand(1);

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ArchiveSubscriptionPlan(1);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task ArchiveSubscriptionPlan_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ArchiveSubscriptionPlanCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.ArchiveSubscriptionPlan(1);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region DeleteSubscriptionPlan Tests

        [Test]
        public async Task DeleteSubscriptionPlan_ShouldReturn200_WhenSuccess()
        {
            var command = new DeleteSubscriptionPlanCommand(1);

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.DeleteSubscriptionPlan(1);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task DeleteSubscriptionPlan_ShouldReturn500_WhenExceptionThrown()
        {
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<DeleteSubscriptionPlanCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.DeleteSubscriptionPlan(1);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region Helper Methods

        private void SetupUserWithRole(string role)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #endregion

    }
}