using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetSchoolAdminDashboardQuery : IRequest<SchoolAdminDashboardResponse>
    {
        public Guid SchoolAdminId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public PeriodType LessonActivityPeriod { get; set; } = PeriodType.Week;
        public PeriodType LessonStatusPeriod { get; set; } = PeriodType.Month;
        public PeriodType ContentTypePeriod { get; set; } = PeriodType.Month;
        public int ReviewLessonsLimit { get; set; } = 7;
        public int TopTeachersLimit { get; set; } = 5;
    }
}