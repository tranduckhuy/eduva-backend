using Eduva.Application.Features.Dashboard.DTOs;

namespace Eduva.Application.Features.Dashboard.Responses
{
    public class SchoolAdminDashboardResponse
    {
        public SchoolAdminSystemOverviewDto SystemOverview { get; set; } = default!;
        public List<LessonActivityDataPoint> LessonActivity { get; set; } = [];
        public List<ReviewLessonDto> ReviewLessons { get; set; } = [];
        public List<ContentTypeStatsDto> ContentTypeStats { get; set; } = [];
        public List<LessonStatusStatsDto> LessonStatusStats { get; set; } = [];
        public List<TopTeachersDto> TopTeachers { get; set; } = [];
    }
}