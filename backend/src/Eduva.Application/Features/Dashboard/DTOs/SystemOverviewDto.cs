namespace Eduva.Application.Features.Dashboard.DTOs
{
    public class SystemOverviewDto
    {
        public int TotalUsers { get; set; }
        public int SchoolAdmins { get; set; }
        public int ContentModerators { get; set; }
        public int Teachers { get; set; }
        public int Students { get; set; }
        public int TotalLessons { get; set; }
        public int UploadedLessons { get; set; }
        public int AIGeneratedLessons { get; set; }
        public int TotalSchools { get; set; }

        // Revenue breakdown
        public decimal CreditPackRevenue { get; set; }
        public decimal SubscriptionPlanRevenue { get; set; }
        public decimal TotalRevenue => CreditPackRevenue + SubscriptionPlanRevenue;
    }

    public class LessonActivityDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public int UploadedCount { get; set; }
        public int AIGeneratedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class TopSchoolItem
    {
        public int SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public int LessonCount { get; set; }
        public int UserCount { get; set; }
        public bool HasActiveSubscription { get; set; }
    }

    public class UserRegistrationDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public int TotalRegistrations { get; set; }
        public int ContentModerators { get; set; }
        public int Teachers { get; set; }
        public int Students { get; set; }
    }

    public class RevenueDataPoint
    {
        public string Period { get; set; } = string.Empty;
        public decimal CreditPackRevenue { get; set; }
        public decimal SubscriptionRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}