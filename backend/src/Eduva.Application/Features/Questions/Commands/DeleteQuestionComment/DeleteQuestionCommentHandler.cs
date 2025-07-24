using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestionComment
{
    public class DeleteQuestionCommentHandler : IRequestHandler<DeleteQuestionCommentCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IQuestionPermissionService _permissionService;
        private readonly INotificationService _notificationService;

        public DeleteQuestionCommentHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IHubNotificationService hubNotificationService,
            IQuestionPermissionService permissionService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(DeleteQuestionCommentCommand request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.DeletedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var comment = await commentRepo.GetByIdAsync(request.Id) ?? throw new AppException(CustomCode.CommentNotFound);

            if (comment.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.CommentNotActive);
            }

            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(comment.QuestionId) ?? throw new AppException(CustomCode.QuestionNotFound);

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lesson = await lessonRepo.GetByIdAsync(question.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            var replies = await ValidateDeletePermissionsAndGetReplies(comment, user, userRole);

            var commentCreator = await userRepo.GetByIdAsync(comment.CreatedByUserId);
            string commentCreatorRole = "";
            if (commentCreator != null)
            {
                var commentCreatorRoles = await _userManager.GetRolesAsync(commentCreator);
                commentCreatorRole = _permissionService.GetHighestPriorityRole(commentCreatorRoles);
            }

            var response = new QuestionCommentResponse
            {
                Id = comment.Id,
                QuestionId = comment.QuestionId,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedByUserId = comment.CreatedByUserId,
                CreatedAt = comment.CreatedAt,
                CreatedByName = commentCreator?.FullName,
                CreatedByAvatar = commentCreator?.AvatarUrl,
                CreatedByRole = commentCreatorRole,
                CanUpdate = false,
                CanDelete = false,
                ReplyCount = replies.Count,
                Replies = new List<QuestionReplyResponse>()
            };

            var targetUserIds = await _notificationService.GetUsersForQuestionCommentNotificationAsync(
                comment.QuestionId, lesson.Id, comment.CreatedByUserId, cancellationToken);


            if (replies.Count != 0)
            {
                foreach (var reply in replies)
                {
                    commentRepo.Remove(reply);
                }
            }

            commentRepo.Remove(comment);

            await _unitOfWork.CommitAsync();



            await _hubNotificationService.NotifyQuestionCommentDeletedAsync(response, lesson.Id, question.Title, lesson.Title, replies.Count, user, targetUserIds);

            return true;
        }

        #region Validation Methods

        private async Task<List<QuestionComment>> ValidateDeletePermissionsAndGetReplies(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var allComments = await commentRepo.GetAllAsync();
            var replies = allComments
                .Where(c => c.ParentCommentId == comment.Id && c.Status == EntityStatus.Active)
                .ToList();

            if (userRole == nameof(Role.SystemAdmin))
            {
                return replies;
            }

            if (replies.Count > 0 && userRole == nameof(Role.Student))
            {
                throw new AppException(CustomCode.CannotDeleteCommentWithReplies);
            }

            if (comment.CreatedByUserId == currentUser.Id)
            {
                return replies;
            }

            var commentCreator = await _unitOfWork.GetRepository<ApplicationUser, Guid>().GetByIdAsync(comment.CreatedByUserId);
            if (commentCreator == null)
            {
                throw new AppException(CustomCode.InsufficientPermissionToDeleteComment);
            }

            var commentCreatorRoles = await _userManager.GetRolesAsync(commentCreator);
            var commentCreatorRole = _permissionService.GetHighestPriorityRole(commentCreatorRoles);

            if (userRole == nameof(Role.SchoolAdmin) &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                return replies;
            }

            if ((userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator)) &&
                commentCreatorRole == nameof(Role.Student) &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                var hasRelationship = await _permissionService.ValidateTeacherStudentRelationshipAsync(currentUser.Id, commentCreator.Id);
                if (hasRelationship)
                {
                    return replies;
                }
            }

            throw new AppException(CustomCode.InsufficientPermissionToDeleteComment);
        }

        #endregion
    }
}