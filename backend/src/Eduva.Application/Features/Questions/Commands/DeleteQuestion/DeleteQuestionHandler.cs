using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
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
        private readonly IQuestionPermissionService _permissionService;

        public DeleteQuestionHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHubNotificationService hubNotificationService, IQuestionPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
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
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            await ValidateDeletePermissions(user, userRole, question);

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var commentCount = await commentRepo.CountAsync(c => c.QuestionId == question.Id, cancellationToken);

            if (commentCount > 0 && userRole == nameof(Role.Student))
            {
                throw new AppException(CustomCode.CannotDeleteQuestionWithComments);
            }

            var lessonMaterialId = question.LessonMaterialId;

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lessonMaterial = await lessonRepo.GetByIdAsync(lessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            if (lessonMaterial.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            questionRepo.Remove(question);
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

            await _hubNotificationService.NotifyQuestionDeletedAsync(response, lessonMaterialId, request.DeletedByUserId);

            return true;
        }

        #region Delete Permissions Validation

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
            var originalCreatorRole = _permissionService.GetHighestPriorityRole(originalCreatorRoles);

            if (userRole == nameof(Role.SchoolAdmin)
                && user.SchoolId.HasValue
                && originalCreator.SchoolId == user.SchoolId)
            {
                return;
            }

            if ((userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator))
                && originalCreatorRole == nameof(Role.Student)
                && user.SchoolId.HasValue
                && originalCreator.SchoolId == user.SchoolId)
            {
                var hasRelationship = await _permissionService.ValidateTeacherStudentRelationshipAsync(user.Id, originalCreator.Id);
                if (hasRelationship)
                {
                    return;
                }
            }

            throw new AppException(CustomCode.InsufficientPermissionToDeleteQuestion);
        }

        #endregion

    }
}