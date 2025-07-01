using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionHandler : IRequestHandler<DeleteQuestionCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;

        public DeleteQuestionHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHubNotificationService hubNotificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
        }

        public async Task<bool> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(request.Id) ?? throw new AppException(CustomCode.QuestionNotFound);

            if (question.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.QuestionNotActive);
            }

            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.DeletedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = GetHighestPriorityRole(roles);

            await ValidateDeletePermissions(user, userRole, question);

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var commentCount = await commentRepo.CountAsync(c => c.QuestionId == question.Id, cancellationToken);

            if (commentCount > 0 && userRole == nameof(Role.Student))
            {
                throw new AppException(CustomCode.CannotDeleteQuestionWithComments);
            }

            var lessonMaterialId = question.LessonMaterialId;

            questionRepo.Remove(question);
            await _unitOfWork.CommitAsync();

            await _hubNotificationService.NotifyQuestionDeletedAsync(request.Id, lessonMaterialId);

            return true;
        }

        #region Role Priority Logic

        private static string GetHighestPriorityRole(IList<string> roles)
        {
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

            return "Unknown";
        }

        #endregion

        #region Delete Permissions Validation

        private async Task ValidateDeletePermissions(ApplicationUser user, string userRole, LessonMaterialQuestion question)
        {
            // SystemAdmin can delete all questions
            if (userRole == nameof(Role.SystemAdmin))
            {
                return;
            }

            // User can delete their own questions
            if (question.CreatedByUserId == user.Id)
            {
                return;
            }

            // Get original creator for additional checks
            var originalCreator = await _unitOfWork.GetRepository<ApplicationUser, Guid>()
                .GetByIdAsync(question.CreatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var originalCreatorRoles = await _userManager.GetRolesAsync(originalCreator);
            var originalCreatorRole = GetHighestPriorityRole(originalCreatorRoles);

            // SchoolAdmin can delete questions in same school (no additional checks)
            if (userRole == nameof(Role.SchoolAdmin)
                && user.SchoolId.HasValue
                && originalCreator.SchoolId == user.SchoolId)
            {
                return;
            }

            // Teacher/ContentModerator can delete Student questions in their class (with teacher-student check)
            if ((userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator))
                && originalCreatorRole == nameof(Role.Student)
                && user.SchoolId.HasValue
                && originalCreator.SchoolId == user.SchoolId)
            {
                await ValidateTeacherStudentRelationship(user.Id, originalCreator.Id);
                return;
            }

            throw new AppException(CustomCode.InsufficientPermissionToDeleteQuestion);
        }

        #endregion

        #region Teacher-Student Relationship Validation

        private async Task ValidateTeacherStudentRelationship(Guid teacherId, Guid studentId)
        {
            var classRepo = _unitOfWork.GetRepository<Classroom, Guid>();
            var teacherClasses = await classRepo.GetAllAsync();
            var teacherClassIds = teacherClasses
                .Where(c => c.TeacherId == teacherId && c.Status == EntityStatus.Active)
                .Select(c => c.Id)
                .ToList();

            if (teacherClassIds.Count == 0)
            {
                throw new AppException(CustomCode.TeacherHasNoActiveClasses);
            }

            var studentClassRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
            var studentClasses = await studentClassRepo.GetClassesForStudentAsync(studentId);
            var studentClassIds = studentClasses.Select(sc => sc.Id).ToList();

            var commonClassIds = teacherClassIds.Intersect(studentClassIds).ToList();
            if (commonClassIds.Count == 0)
            {
                throw new AppException(CustomCode.StudentNotInTeacherClasses);
            }
        }

        #endregion

    }
}