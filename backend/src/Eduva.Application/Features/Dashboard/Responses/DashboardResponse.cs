using Eduva.Application.Features.Dashboard.DTOs;

namespace Eduva.Application.Features.Dashboard.Responses
{
    public class DashboardResponse
    {
        public SystemOverviewDto SystemOverview { get; set; } = new();
        public List<LessonActivityDataPoint> LessonActivity { get; set; } = [];
        public List<TopSchoolItem> TopSchools { get; set; } = [];
        public List<UserRegistrationDataPoint> UserRegistrations { get; set; } = [];
        public List<RevenueDataPoint> RevenueStats { get; set; } = [];
    }
}