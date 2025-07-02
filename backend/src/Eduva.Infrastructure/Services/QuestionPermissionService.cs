using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Infrastructure.Services
{
    public class QuestionPermissionService : IQuestionPermissionService
    {
        private const string UnknownRole = "Unknown";

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuestionPermissionService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public string GetHighestPriorityRole(IList<string> roles)
        {
            if (roles == null || !roles.Any())
            {
                return UnknownRole;
            }

            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                return nameof(Role.SystemAdmin);
            }

            if (roles.Contains(nameof(Role.SchoolAdmin)))
            {
                return nameof(Role.SchoolAdmin);
            }

            if (roles.Contains(nameof(Role.ContentModerator)))
            {
                return nameof(Role.ContentModerator);
            }

            if (roles.Contains(nameof(Role.Teacher)))
            {
                return nameof(Role.Teacher);
            }

            if (roles.Contains(nameof(Role.Student)))
            {
                return nameof(Role.Student);
            }

            return UnknownRole;
        }

        public async Task<string> GetUserRoleSafelyAsync(ApplicationUser? user)
        {
            if (user == null)
            {
                return UnknownRole;
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                return GetHighestPriorityRole(roles);
            }
            catch
            {
                return UnknownRole;
            }
        }

        public bool CanUserUpdateQuestion(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return true;
            }

            return question.CreatedByUserId == currentUser.Id;
        }

        public async Task<bool> CanUserDeleteQuestionAsync(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return true;
            }

            var commentCount = question.Comments?.Count ?? 0;
            if (commentCount > 0 && userRole == nameof(Role.Student))
            {
                return false;
            }

            if (question.CreatedByUserId == currentUser.Id)
            {
                return true;
            }

            var originalCreator = await _unitOfWork.GetRepository<ApplicationUser, Guid>().GetByIdAsync(question.CreatedByUserId);

            if (originalCreator == null)
            {
                return false;
            }

            var originalCreatorRoles = await _userManager.GetRolesAsync(originalCreator);
            var originalCreatorRole = GetHighestPriorityRole(originalCreatorRoles);

            if (userRole == nameof(Role.SchoolAdmin) &&
                currentUser.SchoolId.HasValue &&
                originalCreator.SchoolId == currentUser.SchoolId)
            {
                return true;
            }

            if ((userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator)) &&
                originalCreatorRole == nameof(Role.Student) &&
                currentUser.SchoolId.HasValue &&
                originalCreator.SchoolId == currentUser.SchoolId)
            {
                return await ValidateTeacherStudentRelationshipAsync(currentUser.Id, originalCreator.Id);
            }

            return false;
        }

        public bool CanUserUpdateComment(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return true;
            }

            return comment.CreatedByUserId == currentUser.Id;
        }

        public async Task<bool> CanUserDeleteCommentAsync(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return true;
            }

            var replyCount = comment.Replies?.Count ?? 0;
            if (replyCount > 0 && userRole == nameof(Role.Student))
            {
                return false;
            }

            if (comment.CreatedByUserId == currentUser.Id)
            {
                return true;
            }

            var commentCreator = await _unitOfWork.GetRepository<ApplicationUser, Guid>().GetByIdAsync(comment.CreatedByUserId);

            if (commentCreator == null)
            {
                return false;
            }

            var commentCreatorRoles = await _userManager.GetRolesAsync(commentCreator);
            var commentCreatorRole = GetHighestPriorityRole(commentCreatorRoles);

            if (userRole == nameof(Role.SchoolAdmin) &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                return true;
            }

            if ((userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator)) &&
                commentCreatorRole == nameof(Role.Student) &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                return await ValidateTeacherStudentRelationshipAsync(currentUser.Id, commentCreator.Id);
            }

            return false;
        }

        public async Task<bool> ValidateTeacherStudentRelationshipAsync(Guid teacherId, Guid studentId)
        {
            var classRepo = _unitOfWork.GetRepository<Classroom, Guid>();
            var allClasses = await classRepo.GetAllAsync();
            var teacherClassIds = allClasses
                .Where(c => c.TeacherId == teacherId && c.Status == EntityStatus.Active)
                .Select(c => c.Id)
                .ToList();

            if (teacherClassIds.Count == 0)
            {
                return false;
            }

            var studentClassRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
            var studentClasses = await studentClassRepo.GetClassesForStudentAsync(studentId);
            var studentClassIds = studentClasses.Select(sc => sc.Id).ToList();

            return teacherClassIds.Intersect(studentClassIds).Any();
        }

        public int CalculateTotalCommentCount(ICollection<QuestionComment>? comments)
        {
            if (comments == null || comments.Count == 0)
            {
                return 0;
            }

            var topLevelCount = comments.Count(c => c.ParentCommentId == null);
            var repliesCount = comments.Count(c => c.ParentCommentId != null);

            return topLevelCount + repliesCount;
        }
    }
}