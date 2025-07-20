using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetTeacherDashboardQuery : IRequest<TeacherDashboardResponse>
    {
        public Guid TeacherId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public PeriodType LessonActivityPeriod { get; set; } = PeriodType.Week;
        public PeriodType QuestionVolumePeriod { get; set; } = PeriodType.Week;
        public PeriodType? ContentTypePeriod { get; set; } = null;
        public int ReviewLessonsLimit { get; set; } = 7;
        public int RecentLessonsLimit { get; set; } = 7;
        public int UnAnswerQuestionsLimit { get; set; } = 7;
    }
}