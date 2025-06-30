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
        private readonly IQuestionCommentNotificationService _notificationService;

        public DeleteQuestionHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IQuestionCommentNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.QuestionNotFound);

            if (question.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.QuestionNotActive);
            }

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var commentCount = await commentRepo.CountAsync(c => c.QuestionId == question.Id, cancellationToken);

            if (commentCount > 0)
            {
                throw new AppException(CustomCode.CannotDeleteQuestionWithComments);
            }

            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.DeletedByUserId)
                ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = GetHighestPriorityRole(roles);

            await ValidateDeletePermissions(user, userRole, question);

            var lessonMaterialId = question.LessonMaterialId;

            question.Status = EntityStatus.Deleted;
            question.LastModifiedAt = DateTimeOffset.UtcNow;

            questionRepo.Update(question);
            await _unitOfWork.CommitAsync();

            await _notificationService.NotifyQuestionDeletedAsync(request.Id, lessonMaterialId);

            return true;
        }

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

        private async Task ValidateDeletePermissions(ApplicationUser user, string userRole, LessonMaterialQuestion question)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return;
            }

            if (question.CreatedByUserId == user.Id)
            {
                return;
            }

            var originalCreator = await _unitOfWork.GetRepository<ApplicationUser, Guid>()
                .GetByIdAsync(question.CreatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var originalCreatorRoles = await _userManager.GetRolesAsync(originalCreator);
            var originalCreatorRole = GetHighestPriorityRole(originalCreatorRoles);

            if (question.CreatedByUserId != user.Id)
            {
                if ((userRole == nameof(Role.SchoolAdmin) || userRole == nameof(Role.ContentModerator))
                    && user.SchoolId.HasValue
                    && originalCreator.SchoolId == user.SchoolId)
                {
                    return;
                }

                if (userRole == nameof(Role.Teacher)
                    && originalCreatorRole == nameof(Role.Student)
                    && user.SchoolId.HasValue
                    && originalCreator.SchoolId == user.SchoolId)
                {
                    await ValidateTeacherStudentRelationship(user.Id, originalCreator.Id, question.LessonMaterialId);
                    return;
                }

                throw new AppException(CustomCode.InsufficientPermissionToDeleteQuestion);
            }
        }

        private async Task ValidateTeacherStudentRelationship(Guid teacherId, Guid studentId, Guid lessonMaterialId)
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

            var folderLessonRepo = _unitOfWork.GetRepository<FolderLessonMaterial, Guid>();
            var hasAccessToLesson = await folderLessonRepo.ExistsAsync(flm =>
                flm.LessonMaterialId == lessonMaterialId &&
                flm.Folder.ClassId.HasValue &&
                commonClassIds.Contains(flm.Folder.ClassId.Value) &&
                flm.Folder.Status == EntityStatus.Active);

            if (!hasAccessToLesson)
            {
                throw new AppException(CustomCode.QuestionNotInTeacherClassScope);
            }
        }
    }
}