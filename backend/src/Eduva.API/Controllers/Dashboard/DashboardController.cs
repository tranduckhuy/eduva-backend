using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("overview")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<DashboardResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
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
    }
}