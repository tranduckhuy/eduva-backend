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

        #region GetSchoolAdminDashboard Tests

        [Test]
        public async Task GetSchoolAdminDashboard_ShouldReturn200_WhenSuccessful()
        {
            var response = new SchoolAdminDashboardResponse();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSchoolAdminDashboardQuery>(), default)).ReturnsAsync(response);

            var userId = Guid.NewGuid().ToString();
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) }
                )
            );
            _controller.ControllerContext = controllerContext;

            var result = await _controller.GetSchoolAdminDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetSchoolAdminDashboard_ShouldReturn401_WhenUserIdNotFound()
        {
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity()
            );
            _controller.ControllerContext = controllerContext;

            var result = await _controller.GetSchoolAdminDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetSchoolAdminDashboard_ShouldReturn400_WhenAppExceptionThrown()
        {
            var userId = Guid.NewGuid().ToString();
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) }
                )
            );
            _controller.ControllerContext = controllerContext;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSchoolAdminDashboardQuery>(), default))
                .ThrowsAsync(new AppException(CustomCode.ProvidedInformationIsInValid, ["Invalid data"]));

            var result = await _controller.GetSchoolAdminDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task GetSchoolAdminDashboard_ShouldReturn500_WhenUnexpectedExceptionThrown()
        {
            var userId = Guid.NewGuid().ToString();
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) }
                )
            );
            _controller.ControllerContext = controllerContext;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSchoolAdminDashboardQuery>(), default))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.GetSchoolAdminDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task GetSchoolAdminDashboard_ShouldPassCorrectParameters_WhenCalled()
        {
            var userId = Guid.NewGuid().ToString();
            var controllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controllerContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId) }
                )
            );
            _controller.ControllerContext = controllerContext;

            var startDate = DateTimeOffset.UtcNow.AddDays(-10);
            var endDate = DateTimeOffset.UtcNow;
            GetSchoolAdminDashboardQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSchoolAdminDashboardQuery>(), default))
                .Callback<IRequest<SchoolAdminDashboardResponse>, CancellationToken>((q, _) =>
                {
                    if (q is GetSchoolAdminDashboardQuery query)
                        capturedQuery = query;
                })
                .ReturnsAsync(new SchoolAdminDashboardResponse());

            await _controller.GetSchoolAdminDashboard(startDate, endDate, PeriodType.Month, PeriodType.Year, PeriodType.Week, 10, 3);

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.StartDate, Is.EqualTo(startDate));
                Assert.That(capturedQuery.EndDate, Is.EqualTo(endDate));
                Assert.That(capturedQuery.LessonActivityPeriod, Is.EqualTo(PeriodType.Month));
                Assert.That(capturedQuery.LessonStatusPeriod, Is.EqualTo(PeriodType.Year));
                Assert.That(capturedQuery.ContentTypePeriod, Is.EqualTo(PeriodType.Week));
                Assert.That(capturedQuery.ReviewLessonsLimit, Is.EqualTo(10));
                Assert.That(capturedQuery.TopTeachersLimit, Is.EqualTo(3));
            });
        }

        #endregion

        #region GetTeacherDashboard Tests

        [Test]
        public async Task GetTeacherDashboard_ShouldReturn200_WhenSuccessful()
        {
            var response = new TeacherDashboardResponse();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherDashboardQuery>(), default)).ReturnsAsync(response);

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = user;

            var result = await _controller.GetTeacherDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetTeacherDashboard_ShouldReturn400_WhenAppExceptionThrown()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherDashboardQuery>(), default))
                .ThrowsAsync(new AppException(CustomCode.ProvidedInformationIsInValid, ["Invalid data"]));

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = user;

            var result = await _controller.GetTeacherDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task GetTeacherDashboard_ShouldReturn500_WhenUnexpectedExceptionThrown()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherDashboardQuery>(), default))
                .ThrowsAsync(new Exception("Database error"));

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = user;

            var result = await _controller.GetTeacherDashboard();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task GetTeacherDashboard_ShouldPassCorrectParameters_WhenCalled()
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-5);
            var endDate = DateTimeOffset.UtcNow;
            GetTeacherDashboardQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherDashboardQuery>(), default))
                .Callback<IRequest<TeacherDashboardResponse>, CancellationToken>((q, _) =>
                {
                    if (q is GetTeacherDashboardQuery query)
                        capturedQuery = query;
                })
                .ReturnsAsync(new TeacherDashboardResponse());

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = user;

            await _controller.GetTeacherDashboard(startDate, endDate, PeriodType.Month, PeriodType.Year, PeriodType.Week, 10, 8, 5);

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.StartDate, Is.EqualTo(startDate));
                Assert.That(capturedQuery.EndDate, Is.EqualTo(endDate));
                Assert.That(capturedQuery.LessonActivityPeriod, Is.EqualTo(PeriodType.Month));
                Assert.That(capturedQuery.QuestionVolumePeriod, Is.EqualTo(PeriodType.Year));
                Assert.That(capturedQuery.ContentTypePeriod, Is.EqualTo(PeriodType.Week));
                Assert.That(capturedQuery.ReviewLessonsLimit, Is.EqualTo(10));
                Assert.That(capturedQuery.RecentLessonsLimit, Is.EqualTo(8));
                Assert.That(capturedQuery.UnAnswerQuestionsLimit, Is.EqualTo(5));
            });
        }

        [Test]
        public async Task GetTeacherDashboard_ShouldUseDefaultValues_WhenParametersNotProvided()
        {
            GetTeacherDashboardQuery? capturedQuery = null;
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherDashboardQuery>(), default))
                .Callback<IRequest<TeacherDashboardResponse>, CancellationToken>((q, _) =>
                {
                    if (q is GetTeacherDashboardQuery query)
                        capturedQuery = query;
                })
                .ReturnsAsync(new TeacherDashboardResponse());

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Teacher")
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.User = user;

            await _controller.GetTeacherDashboard();

            Assert.Multiple(() =>
            {
                Assert.That(capturedQuery, Is.Not.Null);
                Assert.That(capturedQuery!.LessonActivityPeriod, Is.EqualTo(PeriodType.Week));
                Assert.That(capturedQuery.QuestionVolumePeriod, Is.EqualTo(PeriodType.Week));
                Assert.That(capturedQuery.ReviewLessonsLimit, Is.EqualTo(7));
                Assert.That(capturedQuery.RecentLessonsLimit, Is.EqualTo(7));
                Assert.That(capturedQuery.UnAnswerQuestionsLimit, Is.EqualTo(7));
            });
        }

        #endregion

    }
}