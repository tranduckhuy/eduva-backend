using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetDashboardQuery : IRequest<DashboardResponse>
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public PeriodType LessonActivityPeriod { get; set; } = PeriodType.Week; // week, month
        public PeriodType UserRegistrationPeriod { get; set; } = PeriodType.Day; // day, month, year
        public PeriodType RevenuePeriod { get; set; } = PeriodType.Month; // month, year
        public int TopSchoolsCount { get; set; } = 7;
    }
}