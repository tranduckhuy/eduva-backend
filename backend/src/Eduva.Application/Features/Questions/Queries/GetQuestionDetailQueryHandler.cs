using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Queries
{
    public class GetQuestionDetailQueryHandler : IRequestHandler<GetQuestionDetailQuery, QuestionDetailResponse>
    {
        private readonly ILessonMaterialQuestionRepository _questionRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetQuestionDetailQueryHandler(ILessonMaterialQuestionRepository questionRepository, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _questionRepository = questionRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<QuestionDetailResponse> Handle(GetQuestionDetailQuery request, CancellationToken cancellationToken)
        {
            var currentUser = await GetAndValidateCurrentUser(request.CurrentUserId);
            var userRole = await GetUserRole(currentUser);

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

        private async Task<string> GetUserRole(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return GetHighestPriorityRole(roles);
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

            response.CreatedByRole = await GetUserRoleSafely(question.CreatedByUser);
            response.CommentCount = CalculateTotalCommentCount(question.Comments);

            response.CanUpdate = CanUserUpdateQuestion(question, currentUser, userRole);
            response.CanDelete = await CanUserDeleteQuestion(question, currentUser, userRole);
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

            response.CreatedByRole = await GetUserRoleSafely(comment.CreatedByUser);
            response.CanUpdate = CanUserUpdateComment(comment, currentUser, userRole);
            response.CanDelete = await CanUserDeleteComment(comment, currentUser, userRole);

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

            response.CreatedByRole = await GetUserRoleSafely(reply.CreatedByUser);
            response.CanUpdate = CanUserUpdateComment(reply, currentUser, userRole);
            response.CanDelete = await CanUserDeleteComment(reply, currentUser, userRole);

            return response;
        }

        #endregion

        #region Permission Helpers - MATCHES Command Logic

        private static bool CanUserUpdateQuestion(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole)
        {
            if (userRole == "SystemAdmin")
            {
                return true;
            }

            return question.CreatedByUserId == currentUser.Id;
        }

        private async Task<bool> CanUserDeleteQuestion(LessonMaterialQuestion question, ApplicationUser currentUser, string userRole)
        {
            if (userRole == "SystemAdmin")
            {
                return true;
            }

            var commentCount = question.Comments?.Count ?? 0;
            if (commentCount > 0 && userRole == "Student")
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

            if (userRole == "SchoolAdmin" &&
                currentUser.SchoolId.HasValue &&
                originalCreator.SchoolId == currentUser.SchoolId)
            {
                return true;
            }

            if ((userRole == "Teacher" || userRole == "ContentModerator") &&
                originalCreatorRole == "Student" &&
                currentUser.SchoolId.HasValue &&
                originalCreator.SchoolId == currentUser.SchoolId)
            {
                return await ValidateTeacherStudentRelationship(currentUser.Id, originalCreator.Id);
            }

            return false;
        }

        private static bool CanUserUpdateComment(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            if (userRole == "SystemAdmin")
            {
                return true;
            }

            return comment.CreatedByUserId == currentUser.Id;
        }

        private async Task<bool> CanUserDeleteComment(QuestionComment comment, ApplicationUser currentUser, string userRole)
        {
            if (userRole == "SystemAdmin")
            {
                return true;
            }

            var replyCount = comment.Replies?.Count ?? 0;
            if (replyCount > 0 && userRole == "Student")
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

            if (userRole == "SchoolAdmin" &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                return true;
            }

            if ((userRole == "Teacher" || userRole == "ContentModerator") &&
                commentCreatorRole == "Student" &&
                currentUser.SchoolId.HasValue &&
                commentCreator.SchoolId == currentUser.SchoolId)
            {
                return await ValidateTeacherStudentRelationship(currentUser.Id, commentCreator.Id);
            }

            return false;
        }

        private async Task<bool> ValidateTeacherStudentRelationship(Guid teacherId, Guid studentId)
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

        #endregion

        #region Helper Methods

        private async Task<string> GetUserRoleSafely(ApplicationUser? user)
        {
            if (user == null)
            {
                return "Unknown";
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                return GetHighestPriorityRole(roles);
            }
            catch
            {
                return "Unknown";
            }
        }

        private static int CalculateTotalCommentCount(ICollection<QuestionComment>? comments)
        {
            if (comments == null || !comments.Any())
            {
                return 0;
            }

            var topLevelCount = comments.Count(c => c.ParentCommentId == null);
            var repliesCount = comments.Count(c => c.ParentCommentId != null);

            return topLevelCount + repliesCount;
        }

        private static string GetHighestPriorityRole(IList<string> roles)
        {
            if (roles == null || !roles.Any())
            {
                return "Unknown";
            }

            if (roles.Contains("SystemAdmin"))
            {
                return "SystemAdmin";
            }

            if (roles.Contains("SchoolAdmin"))
            {
                return "SchoolAdmin";
            }

            if (roles.Contains("ContentModerator"))
            {
                return "ContentModerator";
            }

            if (roles.Contains("Teacher"))
            {
                return "Teacher";
            }

            if (roles.Contains("Student"))
            {
                return "Student";
            }

            return "Unknown";
        }

        #endregion
    }
}