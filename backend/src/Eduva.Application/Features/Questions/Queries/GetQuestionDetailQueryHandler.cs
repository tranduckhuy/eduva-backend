using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Questions.Queries
{
    public class GetQuestionDetailQueryHandler : IRequestHandler<GetQuestionDetailQuery, QuestionDetailResponse>
    {
        private readonly ILessonMaterialQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionPermissionService _permissionService;

        public GetQuestionDetailQueryHandler(ILessonMaterialQuestionRepository questionRepository, IUnitOfWork unitOfWork, IQuestionPermissionService permissionService)
        {
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
        }

        public async Task<QuestionDetailResponse> Handle(GetQuestionDetailQuery request, CancellationToken cancellationToken)
        {
            var currentUser = await GetAndValidateCurrentUser(request.CurrentUserId);
            var userRole = await _permissionService.GetUserRoleSafelyAsync(currentUser);

            await ValidateQuestionAccess(request.QuestionId, request.CurrentUserId, userRole);

            var question = await GetQuestionWithDetails(request.QuestionId);

            ValidateLessonMaterialRules(question.LessonMaterial!, currentUser);

            var response = await BuildQuestionDetailResponseWithMapping(question, currentUser, userRole);

            return response;
        }

        #region Validation and Data Retrieval

        private async Task<ApplicationUser> GetAndValidateCurrentUser(Guid userId)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(userId);

            return user ?? throw new AppException(CustomCode.UserNotFound);
        }

        private async Task ValidateQuestionAccess(Guid questionId, Guid userId, string userRole)
        {
            var hasAccess = await _questionRepository.IsQuestionAccessibleToUserAsync(questionId, userId, userRole);

            if (!hasAccess)
            {
                throw new AppException(CustomCode.QuestionNotFound);
            }
        }

        private async Task<LessonMaterialQuestion> GetQuestionWithDetails(Guid questionId)
        {
            var question = await _questionRepository.GetQuestionWithFullDetailsAsync(questionId);

            return question ?? throw new AppException(CustomCode.QuestionNotFound);
        }

        private static void ValidateLessonMaterialRules(LessonMaterial lessonMaterial, ApplicationUser currentUser)
        {
            if (lessonMaterial.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            if (lessonMaterial.LessonStatus != LessonMaterialStatus.Approved)
            {
                throw new AppException(CustomCode.CannotCreateQuestionForPendingLesson);
            }

            if (currentUser.SchoolId != lessonMaterial.SchoolId)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }
        }

        #endregion

        #region Response Building with AutoMapper

        private async Task<QuestionDetailResponse> BuildQuestionDetailResponseWithMapping(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole)
        {
            var response = AppMapper.Mapper.Map<QuestionDetailResponse>(question);

            response.CreatedByRole = await _permissionService.GetUserRoleSafelyAsync(question.CreatedByUser);
            response.CommentCount = _permissionService.CalculateTotalCommentCount(question.Comments);

            response.CanUpdate = _permissionService.CanUserUpdateQuestion(question, currentUser, userRole);
            response.CanDelete = await _permissionService.CanUserDeleteQuestionAsync(question, currentUser, userRole);
            response.CanComment = true;

            response.Comments = await BuildCommentStructureWithMapping(question.Comments, currentUser, userRole);

            return response;
        }

        private async Task<List<QuestionCommentResponse>> BuildCommentStructureWithMapping(ICollection<QuestionComment>? comments, ApplicationUser currentUser, string userRole)
        {
            if (comments == null || comments.Count == 0)
            {
                return [];
            }

            var result = new List<QuestionCommentResponse>();

            var topLevelComments = comments
                .Where(c => c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            foreach (var comment in topLevelComments)
            {
                var commentResponse = await BuildCommentResponseWithMapping(comment, currentUser, userRole);
                result.Add(commentResponse);
            }

            return result;
        }

        private async Task<QuestionCommentResponse> BuildCommentResponseWithMapping(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            var response = AppMapper.Mapper.Map<QuestionCommentResponse>(comment);

            response.CreatedByRole = await _permissionService.GetUserRoleSafelyAsync(comment.CreatedByUser);
            response.CanUpdate = _permissionService.CanUserUpdateComment(comment, currentUser, userRole);
            response.CanDelete = await _permissionService.CanUserDeleteCommentAsync(comment, currentUser, userRole);

            response.Replies = await BuildRepliesWithMapping(comment.Replies, currentUser, userRole);
            response.ReplyCount = response.Replies.Count;

            return response;
        }

        private async Task<List<QuestionReplyResponse>> BuildRepliesWithMapping(ICollection<QuestionComment>? replies, ApplicationUser currentUser, string userRole)
        {
            if (replies == null || replies.Count == 0)
            {
                return [];
            }

            var result = new List<QuestionReplyResponse>();

            foreach (var reply in replies.OrderBy(r => r.CreatedAt))
            {
                var replyResponse = await BuildReplyResponseWithMapping(reply, currentUser, userRole);
                result.Add(replyResponse);
            }

            return result;
        }

        private async Task<QuestionReplyResponse> BuildReplyResponseWithMapping(QuestionComment reply, ApplicationUser currentUser, string userRole)
        {
            var response = AppMapper.Mapper.Map<QuestionReplyResponse>(reply);

            response.CreatedByRole = await _permissionService.GetUserRoleSafelyAsync(reply.CreatedByUser);
            response.CanUpdate = _permissionService.CanUserUpdateComment(reply, currentUser, userRole);
            response.CanDelete = await _permissionService.CanUserDeleteCommentAsync(reply, currentUser, userRole);

            return response;
        }

        #endregion

    }
}