using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Dashboard
{
    [Route("api/dashboards")]
    public class DashboardController : BaseController<DashboardController>
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator, ILogger<DashboardController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("system-admin")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<DashboardResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] PeriodType lessonActivityPeriod = PeriodType.Week,
            [FromQuery] PeriodType userRegistrationPeriod = PeriodType.Day,
            [FromQuery] PeriodType revenuePeriod = PeriodType.Month,
            [FromQuery] int topSchoolsCount = 7)
        {
            return await HandleRequestAsync<DashboardResponse>(async () =>
            {
                var query = new GetDashboardQuery
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    LessonActivityPeriod = lessonActivityPeriod,
                    UserRegistrationPeriod = userRegistrationPeriod,
                    RevenuePeriod = revenuePeriod,
                    TopSchoolsCount = topSchoolsCount
                };

                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("school-admin")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(ApiResponse<SchoolAdminDashboardResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolAdminDashboard(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] PeriodType lessonActivityPeriod = PeriodType.Week,
            [FromQuery] PeriodType lessonStatusPeriod = PeriodType.Month,
            [FromQuery] PeriodType contentTypePeriod = PeriodType.Month,
            [FromQuery] int reviewLessonsLimit = 7,
            [FromQuery] int topTeachersLimit = 5)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var schoolAdminId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetSchoolAdminDashboardQuery
            {
                SchoolAdminId = schoolAdminId,
                StartDate = startDate,
                EndDate = endDate,
                LessonActivityPeriod = lessonActivityPeriod,
                LessonStatusPeriod = lessonStatusPeriod,
                ContentTypePeriod = contentTypePeriod,
                ReviewLessonsLimit = reviewLessonsLimit,
                TopTeachersLimit = topTeachersLimit
            };

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("teacher")]
        [Authorize(Roles = $"{nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}")]
        [ProducesResponseType(typeof(ApiResponse<TeacherDashboardResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeacherDashboard(
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] PeriodType lessonActivityPeriod = PeriodType.Week,
            [FromQuery] PeriodType questionVolumePeriod = PeriodType.Week,
            [FromQuery] PeriodType contentTypePeriod = PeriodType.Month,
            [FromQuery] int reviewLessonsLimit = 7,
            [FromQuery] int recentLessonsLimit = 7,
            [FromQuery] int unAnswerQuestionsLimit = 7)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var teacherId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetTeacherDashboardQuery
            {
                TeacherId = teacherId,
                StartDate = startDate,
                EndDate = endDate,
                LessonActivityPeriod = lessonActivityPeriod,
                QuestionVolumePeriod = questionVolumePeriod,
                ContentTypePeriod = contentTypePeriod,
                ReviewLessonsLimit = reviewLessonsLimit,
                RecentLessonsLimit = recentLessonsLimit,
                UnAnswerQuestionsLimit = unAnswerQuestionsLimit
            };

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }
    }
}