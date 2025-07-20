using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Dashboard.DTOs
{
    public class SchoolAdminSystemOverviewDto
    {
        public int TotalUsers { get; set; }
        public int SchoolAdmin { get; set; }
        public int ContentModerators { get; set; }
        public int Teachers { get; set; }
        public int Students { get; set; }
        public int Classes { get; set; }
        public int TotalLessons { get; set; }
        public int UploadedLessons { get; set; }
        public int AIGeneratedLessons { get; set; }
        public long UsedStorageBytes { get; set; }
        public double UsedStorageGB { get; set; }
        public double StorageUsagePercentage { get; set; }
        public CurrentSubscriptionDto CurrentSubscription { get; set; } = new();
    }

    public class CurrentSubscriptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public long MaxStorageBytes { get; set; }
        public double MaxStorageGB { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class ReviewLessonDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }
        public LessonMaterialStatus LessonStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class TopTeachersDto
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public int LessonCount { get; set; }
        public int ClassesCount { get; set; }
    }

    public class ContentTypeStatsDto
    {
        public string Period { get; set; } = string.Empty;
        public int Pdf { get; set; }
        public int Doc { get; set; }
        public int Video { get; set; }
        public int Audio { get; set; }
        public int Total { get; set; }
        public double PdfPercentage { get; set; }
        public double DocPercentage { get; set; }
        public double VideoPercentage { get; set; }
        public double AudioPercentage { get; set; }
    }

    public class LessonStatusStatsDto
    {
        public string Period { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public double PendingPercentage { get; set; }
        public double ApprovedPercentage { get; set; }
        public double RejectedPercentage { get; set; }
    }
}