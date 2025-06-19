using Eduva.API.Controllers.SubscriptionPlans;
using Eduva.Application.Common.Models;
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

        #region SubscriptionPlanControllerTests Tests

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