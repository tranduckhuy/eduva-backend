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
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestionComment
{
    public class CreateQuestionCommentHandler : IRequestHandler<CreateQuestionCommentCommand, QuestionCommentResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubNotificationService _hubNotificationService;
        private readonly IQuestionPermissionService _permissionService;

        public CreateQuestionCommentHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IHubNotificationService hubNotificationService, IQuestionPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubNotificationService = hubNotificationService;
            _permissionService = permissionService;
        }

        public async Task<QuestionCommentResponse> Handle(CreateQuestionCommentCommand request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.CreatedByUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(roles);

            var questionRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
            var question = await questionRepo.GetByIdAsync(request.QuestionId) ?? throw new AppException(CustomCode.QuestionNotFound);

            if (question.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.QuestionNotActive);
            }

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lesson = await lessonRepo.GetByIdAsync(question.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);


            await ValidateQuestionAccessPermissions(user, userRole, question);

            Guid? flattenedParentCommentId = null;

            if (request.ParentCommentId.HasValue)
            {
                var parentComment = await ValidateAndGetFlattenedParent(request.ParentCommentId.Value, request.QuestionId);

                flattenedParentCommentId = parentComment.ParentCommentId ?? parentComment.Id;
            }

            var comment = new QuestionComment
            {
                Id = Guid.NewGuid(),
                QuestionId = request.QuestionId,
                Content = request.Content.Trim(),
                ParentCommentId = flattenedParentCommentId,
                CreatedByUserId = request.CreatedByUserId,
                Status = EntityStatus.Active
            };

            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            await commentRepo.AddAsync(comment);
            await _unitOfWork.CommitAsync();

            var response = BuildCommentResponse(comment, user, userRole);

            await _hubNotificationService.NotifyQuestionCommentedAsync(response, question.LessonMaterialId, question.Title, lesson.Title, user);

            return response;
        }

        #region Validation Methods

        private async Task ValidateQuestionAccessPermissions(ApplicationUser user, string userRole, LessonMaterialQuestion question)
        {
            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lesson = await lessonRepo.GetByIdAsync(question.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

            if (lesson.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.LessonMaterialNotActive);
            }

            if (lesson.LessonStatus != LessonMaterialStatus.Approved)
            {
                throw new AppException(CustomCode.CannotCreateQuestionForPendingLesson);
            }

            if (user.SchoolId != lesson.SchoolId)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }

            await ValidateRoleBasedAccess(user.Id, question.LessonMaterialId, userRole, question);
        }

        private async Task ValidateRoleBasedAccess(Guid userId, Guid lessonMaterialId, string userRole, LessonMaterialQuestion question)
        {

            if (question.CreatedByUserId == userId)
            {
                return;
            }

            var studentClassRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

            switch (userRole)
            {
                case nameof(Role.Student):
                    var hasAccess = await studentClassRepo.HasAccessToMaterialAsync(userId, lessonMaterialId);
                    if (!hasAccess)
                    {
                        var isEnrolled = await studentClassRepo.IsEnrolledInAnyClassAsync(userId);
                        if (!isEnrolled)
                        {
                            throw new AppException(CustomCode.StudentNotEnrolledInAnyClassForQuestions);
                        }
                        throw new AppException(CustomCode.CannotCreateQuestionForLessonNotAccessible);
                    }
                    break;

                case nameof(Role.Teacher):
                    var teacherHasAccess = await studentClassRepo.TeacherHasAccessToMaterialAsync(userId, lessonMaterialId);
                    if (!teacherHasAccess)
                    {
                        throw new AppException(CustomCode.TeacherNotHaveAccessToMaterial);
                    }
                    break;

                case nameof(Role.ContentModerator):
                case nameof(Role.SchoolAdmin):
                case nameof(Role.SystemAdmin):
                    break;

                default:
                    throw new AppException(CustomCode.InsufficientPermissionToCreateComment);
            }
        }

        private async Task<QuestionComment> ValidateAndGetFlattenedParent(Guid parentCommentId, Guid questionId)
        {
            var commentRepo = _unitOfWork.GetRepository<QuestionComment, Guid>();
            var parentComment = await commentRepo.GetByIdAsync(parentCommentId) ?? throw new AppException(CustomCode.ParentCommentNotFound);

            if (parentComment.QuestionId != questionId)
            {
                throw new AppException(CustomCode.ParentCommentNotFound);
            }

            if (parentComment.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.CommentNotActive);
            }

            return parentComment;
        }

        #endregion

        #region Response Building

        private static QuestionCommentResponse BuildCommentResponse(QuestionComment comment, ApplicationUser user, string userRole)
        {
            var response = AppMapper<AppMappingProfile>.Mapper.Map<QuestionCommentResponse>(comment);

            response.CreatedByName = user.FullName;
            response.CreatedByAvatar = user.AvatarUrl;
            response.CreatedByRole = userRole;
            response.CanUpdate = true;
            response.CanDelete = true;
            response.ReplyCount = 0;
            response.Replies = new List<QuestionReplyResponse>();

            return response;
        }

        #endregion
    }
}