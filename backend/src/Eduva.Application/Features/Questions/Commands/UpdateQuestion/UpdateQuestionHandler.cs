using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionHandler : IRequestHandler<UpdateQuestionCommand, QuestionResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IQuestionPermissionService _permissionService;

        public UpdateQuestionHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHubNotificationService hubNotificationService, IQuestionPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
        }

        public async Task<QuestionResponse> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
        {
            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(request.Id) ?? throw new AppException(CustomCode.QuestionNotFound);

            if (question.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.QuestionNotActive);
            }

            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.UpdatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            ValidateUpdatePermissions(user, userRole, question);

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lessonMaterial = await lessonRepo.GetByIdAsync(question.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            if (lessonMaterial.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            question.Title = request.Title;
            question.Content = request.Content;
            question.LastModifiedAt = DateTimeOffset.UtcNow;

            questionRepo.Update(question);
            await _unitOfWork.CommitAsync();

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var commentCount = await commentRepo.CountAsync(c => c.QuestionId == question.Id, cancellationToken);

            var creatorRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var creator = await creatorRepo.GetByIdAsync(question.CreatedByUserId);
            var creatorRoles = creator != null ? await _userManager.GetRolesAsync(creator) : new List<string>();
            var creatorRole = _permissionService.GetHighestPriorityRole(creatorRoles);

            var response = new QuestionResponse
            {
                Id = question.Id,
                LessonMaterialId = question.LessonMaterialId,
                LessonMaterialTitle = lessonMaterial.Title,
                Title = question.Title,
                Content = question.Content,
                CreatedAt = question.CreatedAt,
                LastModifiedAt = question.LastModifiedAt,
                CreatedByUserId = question.CreatedByUserId,
                CreatedByName = creator?.FullName,
                CreatedByAvatar = creator?.AvatarUrl,
                CreatedByRole = creatorRole,
                CommentCount = commentCount
            };

            await _hubNotificationService.NotifyQuestionUpdatedAsync(response, question.LessonMaterialId, request.UpdatedByUserId);

            return response;
        }

        #region Validation Logic

        private static void ValidateUpdatePermissions(ApplicationUser user, string userRole, LessonMaterialQuestion question)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return;
            }

            if (question.CreatedByUserId != user.Id)
            {
                throw new AppException(CustomCode.InsufficientPermissionToUpdateQuestion);
            }
        }

        #endregion
    }
}