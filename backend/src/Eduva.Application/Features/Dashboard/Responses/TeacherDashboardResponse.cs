using Eduva.Application.Features.Dashboard.DTOs;

namespace Eduva.Application.Features.Dashboard.Responses
{
    public class TeacherDashboardResponse
    {
        public TeacherSystemOverviewDto SystemOverview { get; set; } = default!;
        public List<LessonActivityDataPoint> LessonActivity { get; set; } = [];
        public List<QuestionVolumeTrendDto> QuestionVolumeTrend { get; set; } = [];
        public List<ContentTypeStatsDto> ContentTypeStats { get; set; } = [];
        public List<ReviewLessonDto> ReviewLessons { get; set; } = [];
        public List<RecentLessonDto> RecentLessons { get; set; } = [];
        public List<UnAnswerQuestionDto> UnAnswerQuestions { get; set; } = [];
    }
}