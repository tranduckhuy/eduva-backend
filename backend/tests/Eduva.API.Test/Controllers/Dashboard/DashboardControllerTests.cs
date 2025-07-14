using Eduva.API.Controllers.Dashboard;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.API.Test.Controllers.Dashboard
{
    [TestFixture]
    public class DashboardControllerTests
    {
        private Mock<IMediator> _mediatorMock = null!;
        private Mock<ILogger<DashboardController>> _loggerMock = null!;
        private DashboardController _controller = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<DashboardController>>();
            _controller = new DashboardController(_mediatorMock.Object, _loggerMock.Object);
        }

        #endregion

        #region Tests

        [Test]
        public async Task GetDashboard_ShouldReturn200_WhenSuccessful()
        {
            var response = new DashboardResponse
            {
                SystemOverview = new SystemOverviewDto { TotalUsers = 100 },
                LessonActivity = [],
                TopSchools = [],
                UserRegistrations = [],
                RevenueStats = []
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default)).ReturnsAsync(response);

            var result = await _controller.GetDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetDashboard_ShouldPassCorrectParameters_WhenCalled()
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-30);
            var endDate = DateTimeOffset.UtcNow;
            GetDashboardQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .Callback<IRequest<DashboardResponse>, CancellationToken>((q, _) =>
                        {
                            if (q is GetDashboardQuery query)
                                capturedQuery = query;
                        })
                        .ReturnsAsync(new DashboardResponse());

            await _controller.GetDashboard(startDate, endDate, PeriodType.Month, PeriodType.Year, PeriodType.Year, 15);

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.StartDate, Is.EqualTo(startDate));
                Assert.That(capturedQuery.EndDate, Is.EqualTo(endDate));
                Assert.That(capturedQuery.LessonActivityPeriod, Is.EqualTo(PeriodType.Month));
                Assert.That(capturedQuery.TopSchoolsCount, Is.EqualTo(15));
            });
        }

        [Test]
        public async Task GetDashboard_ShouldReturn400_WhenAppExceptionThrown()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .ThrowsAsync(new AppException(CustomCode.ProvidedInformationIsInValid, ["Invalid data"]));

            var result = await _controller.GetDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task GetDashboard_ShouldReturn500_WhenUnexpectedExceptionThrown()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.GetDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task GetDashboard_ShouldReturn400_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("topSchoolsCount", "Invalid value");

            var result = await _controller.GetDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task GetDashboard_ShouldUseDefaultValues_WhenParametersNotProvided()
        {
            GetDashboardQuery? capturedQuery = null;
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .Callback<IRequest<DashboardResponse>, CancellationToken>((q, _) =>
                        {
                            if (q is GetDashboardQuery query)
                                capturedQuery = query;
                        })
                        .ReturnsAsync(new DashboardResponse());

            await _controller.GetDashboard();

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.LessonActivityPeriod, Is.EqualTo(PeriodType.Week));
                Assert.That(capturedQuery.UserRegistrationPeriod, Is.EqualTo(PeriodType.Day));
                Assert.That(capturedQuery.RevenuePeriod, Is.EqualTo(PeriodType.Month));
                Assert.That(capturedQuery.TopSchoolsCount, Is.EqualTo(7));
            });
        }

        [Test]
        public async Task GetDashboard_ShouldHandleEmptyResponse_Successfully()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .ReturnsAsync(new DashboardResponse());

            var result = await _controller.GetDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetDashboard_ShouldHandleNullDates_Successfully()
        {
            GetDashboardQuery? capturedQuery = null;
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetDashboardQuery>(), default))
                        .Callback<IRequest<DashboardResponse>, CancellationToken>((q, _) =>
                        {
                            if (q is GetDashboardQuery query)
                                capturedQuery = query;
                        })
                        .ReturnsAsync(new DashboardResponse());

            await _controller.GetDashboard(null, null);

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.StartDate, Is.Null);
                Assert.That(capturedQuery.EndDate, Is.Null);
            });
        }

        #endregion

    }
}