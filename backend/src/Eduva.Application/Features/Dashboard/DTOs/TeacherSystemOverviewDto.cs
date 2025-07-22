using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Dashboard.DTOs
{
    public class TeacherSystemOverviewDto
    {
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalLessons { get; set; }
        public int UploadedLessons { get; set; }
        public int AIGeneratedLessons { get; set; }
        public int TotalPendingLessons { get; set; }
        public int RemainCreditPoints { get; set; }
        public int UnansweredQuestions { get; set; }
        public long UsedStorageBytes { get; set; }
        public double UsedStorageGB { get; set; }
    }

    public class QuestionVolumeTrendDto
    {
        public string Period { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TotalAnswers { get; set; }
        public int Total { get; set; }
    }

    public class RecentLessonDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ContentType ContentType { get; set; }
        public LessonMaterialStatus LessonStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string OwnerName { get; set; } = string.Empty;
    }

    public class UnAnswerQuestionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
    }
}