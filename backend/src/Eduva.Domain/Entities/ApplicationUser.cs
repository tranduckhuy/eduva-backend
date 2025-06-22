using Eduva.Domain.Constants;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
        public string AvatarUrl { get; set; } = AppConstants.DEFAULT_AVATAR;
        public int? SchoolId { get; set; }
        public EntityStatus Status { get; set; } = EntityStatus.Active;

        public int TotalCredits { get; set; } = 0;
        public string? RefreshToken { get; set; }
        public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
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
        public virtual ICollection<UserCreditTransaction> CreditTransactions { get; set; } = [];
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
    }
}