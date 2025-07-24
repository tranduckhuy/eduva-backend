using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestion
{
    public class CreateQuestionHandler : IRequestHandler<CreateQuestionCommand, QuestionResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IQuestionPermissionService _permissionService;

        public CreateQuestionHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHubNotificationService hubNotificationService, IQuestionPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
        }

        public async Task<QuestionResponse> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.CreatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            if (!IsAllowedToCreateQuestion(userRole))
            {
                throw new AppException(CustomCode.InsufficientPermissionToCreateQuestion);
            }

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lessonMaterial = await lessonRepo.GetByIdAsync(request.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            if (lessonMaterial.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            await ValidateRoleBasedPermissions(user, userRole, lessonMaterial);

            var question = new LessonMaterialQuestion
            {
                Id = Guid.NewGuid(),
                LessonMaterialId = request.LessonMaterialId,
                Title = request.Title,
                Content = request.Content,
                CreatedByUserId = request.CreatedByUserId,
                Status = EntityStatus.Active
            };

            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            await questionRepo.AddAsync(question);
            await _unitOfWork.CommitAsync();

            var response = new QuestionResponse
            {
                Id = question.Id,
                LessonMaterialId = question.LessonMaterialId,
                LessonMaterialTitle = lessonMaterial.Title,
                Title = question.Title,
                Content = question.Content,
                CreatedAt = question.CreatedAt,
                CreatedByUserId = question.CreatedByUserId,
                CreatedByName = user.FullName,
                CreatedByAvatar = user.AvatarUrl,
                CreatedByRole = userRole,
                CommentCount = 0
            };

            await _hubNotificationService.NotifyQuestionCreatedAsync(response, request.LessonMaterialId, user);

            return response;
        }

        #region Role Permission Checks

        private static bool IsAllowedToCreateQuestion(string primaryRole)
        {
            return primaryRole == nameof(Role.Teacher) ||
                   primaryRole == nameof(Role.ContentModerator) ||
                   primaryRole == nameof(Role.Student);
        }

        #endregion

        #region Role-Based Permission Validation

        private async Task ValidateRoleBasedPermissions(ApplicationUser user, string primaryRole, LessonMaterial lessonMaterial)
        {
            switch (primaryRole)
            {
                case nameof(Role.Teacher):
                case nameof(Role.ContentModerator):
                    ValidateTeacherContentModeratorAccess(user, lessonMaterial);
                    break;

                case nameof(Role.Student):
                    await ValidateStudentAccess(user.Id, lessonMaterial.Id);
                    break;

                default:
                    throw new AppException(CustomCode.InsufficientPermission);
            }
        }

        #endregion

        #region Teacher and Content Moderator Access Validation

        private static void ValidateTeacherContentModeratorAccess(ApplicationUser user, LessonMaterial lessonMaterial)
        {
            if (user.SchoolId == null)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }

            if (lessonMaterial.SchoolId != user.SchoolId)
            {
                throw new AppException(CustomCode.CannotCreateQuestionForLessonNotInYourSchool);
            }

            if (lessonMaterial.LessonStatus == LessonMaterialStatus.Pending)
            {
                throw new AppException(CustomCode.CannotCreateQuestionForPendingLesson);
            }
        }

        #endregion

        #region Student Access Validation

        private async Task ValidateStudentAccess(Guid studentId, Guid lessonMaterialId)
        {
            var studentClassCustomRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
            var enrolledClasses = await studentClassCustomRepo.GetClassesForStudentAsync(studentId);

            if (enrolledClasses.Count == 0)
            {
                throw new AppException(CustomCode.StudentNotEnrolledInAnyClass);
            }

            var enrolledClassIds = enrolledClasses.Select(c => c.Id).ToList();

            var folderLessonRepo = _unitOfWork.GetRepository<FolderLessonMaterial, Guid>();
            var hasAccess = await folderLessonRepo.ExistsAsync(flm =>
                flm.LessonMaterialId == lessonMaterialId &&
                flm.Folder.ClassId.HasValue &&
                enrolledClassIds.Contains(flm.Folder.ClassId.Value) &&
                flm.Folder.Status == EntityStatus.Active);

            if (!hasAccess)
            {
                throw new AppException(CustomCode.CannotCreateQuestionForLessonNotAccessible);
            }
        }

        #endregion
    }
}