using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public int? SchoolId { get; set; }
        public DateTimeOffset? DOB { get; set; }
        public EntityStatus Status { get; set; }

        public string? RefreshToken { get; set; }
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        public School? School { get; set; } = null!;

        public virtual ICollection<AIUsageLog> AIUsageLogs { get; set; } = [];
        public virtual ICollection<Classroom> ClassesAsTeacher { get; set; } = [];
        public virtual ICollection<StudentClass> StudentClasses { get; set; } = [];
        public virtual ICollection<Folder> PersonalFolders { get; set; } = [];
        public virtual ICollection<LessonMaterial> CreatedLessonMaterials { get; set; } = [];
        public virtual ICollection<LessonMaterialApproval> ApprovedLessonMaterials { get; set; } = [];
        public virtual ICollection<LessonMaterialQuestion> CreatedLessonMaterialQuestions { get; set; } = [];
        public virtual ICollection<QuestionComment> CreatedQuestionComments { get; set; } = [];
        public virtual ICollection<UserNotification> ReceivedNotifications { get; set; } = [];
    }
}
