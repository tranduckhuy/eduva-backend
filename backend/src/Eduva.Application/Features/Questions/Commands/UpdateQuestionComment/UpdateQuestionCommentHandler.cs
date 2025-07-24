using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestionComment
{
    public class UpdateQuestionCommentHandler : IRequestHandler<UpdateQuestionCommentCommand, QuestionCommentResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IQuestionPermissionService _permissionService;

        public UpdateQuestionCommentHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IHubNotificationService hubNotificationService,
            IQuestionPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
        }

        public async Task<QuestionCommentResponse> Handle(UpdateQuestionCommentCommand request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.UpdatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var comment = await GetCommentWithDetailsAsync(request.Id) ?? throw new AppException(CustomCode.CommentNotFound);
            if (comment.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.CommentNotActive);
            }

            ValidateUpdatePermissions(comment, user, userRole);

            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(comment.QuestionId) ?? throw new AppException(CustomCode.QuestionNotFound);

            comment.Content = request.Content.Trim();
            comment.LastModifiedAt = DateTimeOffset.UtcNow;

            commentRepo.Update(comment);
            await _unitOfWork.CommitAsync();

            var response = await BuildCommentResponseWithPermissions(comment, user, userRole);
            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lesson = await lessonRepo.GetByIdAsync(question.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);
            if (lesson.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            await _hubNotificationService.NotifyQuestionCommentUpdatedAsync(response, question.LessonMaterialId, question.Title, lesson.Title, user);

            return response;
        }

        #region Data Retrieval

        private async Task<QuestionComment?> GetCommentWithDetailsAsync(Guid commentId)
        {
            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();

            var comment = await commentRepo.GetByIdAsync(commentId);

            if (comment == null)
            {
                return null;
            }

            var allComments = await commentRepo.GetAllAsync();
            var replies = allComments
                .Where(c => c.ParentCommentId == commentId && c.Status == EntityStatus.Active)
                .ToList();

            comment.Replies ??= replies;

            return comment;
        }

        #endregion

        #region Validation Methods

        private static void ValidateUpdatePermissions(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            if (userRole == nameof(Role.SystemAdmin))
            {
                return;
            }

            if (comment.CreatedByUserId == currentUser.Id)
            {
                return;
            }

            throw new AppException(CustomCode.InsufficientPermissionToUpdateComment);
        }

        #endregion

        #region Response Building

        private async Task<QuestionCommentResponse> BuildCommentResponseWithPermissions(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            var response = AppMapper<AppMappingProfile>.Mapper.Map<QuestionCommentResponse>(comment);

            response.CreatedByName = comment.CreatedByUser?.FullName ?? currentUser.FullName;
            response.CreatedByAvatar = comment.CreatedByUser?.AvatarUrl ?? currentUser.AvatarUrl;
            response.CreatedByRole = await _permissionService.GetUserRoleSafelyAsync(comment.CreatedByUser ?? currentUser);

            response.CanUpdate = _permissionService.CanUserUpdateComment(comment, currentUser, userRole);
            response.CanDelete = await _permissionService.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            response.ReplyCount = comment.Replies?.Count ?? 0;
            response.Replies = new List<QuestionReplyResponse>();

            return response;
        }

        #endregion

    }
}