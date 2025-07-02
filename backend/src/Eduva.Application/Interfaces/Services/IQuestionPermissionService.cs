using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Services
{
    public interface IQuestionPermissionService
    {
        string GetHighestPriorityRole(IList<string> roles);

        Task<string> GetUserRoleSafelyAsync(ApplicationUser? user);

        bool CanUserUpdateQuestion(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole);

        Task<bool> CanUserDeleteQuestionAsync(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole);

        bool CanUserUpdateComment(QuestionComment comment, ApplicationUser currentUser, string userRole);

        Task<bool> CanUserDeleteCommentAsync(QuestionComment comment, ApplicationUser currentUser, string userRole);

        Task<bool> ValidateTeacherStudentRelationshipAsync(Guid teacherId, Guid studentId);

        int CalculateTotalCommentCount(ICollection<QuestionComment>? comments);
    }
}