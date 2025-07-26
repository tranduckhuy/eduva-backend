using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Queries
{
    public class GetQuestionsByLessonQueryHandler
        : IRequestHandler<GetQuestionsByLessonQuery, Pagination<QuestionResponse>>
    {
        private readonly ILessonMaterialQuestionRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionPermissionService _permissionService;

        public GetQuestionsByLessonQueryHandler(ILessonMaterialQuestionRepository repository, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IQuestionPermissionService permissionService)
        {
            _repository = repository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
        }

        public async Task<Pagination<QuestionResponse>> Handle(GetQuestionsByLessonQuery request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.CurrentUserId) ?? throw new AppException(CustomCode.UserNotFound);

            var lessonRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lesson = await lessonRepo.GetByIdAsync(request.LessonMaterialId) ?? throw new AppException(CustomCode.LessonMaterialNotFound);

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

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = _permissionService.GetHighestPriorityRole(userRoles);

            await ValidateUserAccessToMaterial(request.CurrentUserId, request.LessonMaterialId, userRole, lesson);

            var totalCount = await _repository.CountAsync(q =>
                q.LessonMaterialId == request.LessonMaterialId
                && q.LessonMaterial.SchoolId == user.SchoolId, cancellationToken);

            var spec = new QuestionsByLessonSpecification(request.Param, request.LessonMaterialId, user.SchoolId);
            var result = await _repository.GetWithSpecAsync(spec);
            var response = AppMapper<AppMappingProfile>.Mapper.Map<Pagination<QuestionResponse>>(result);

            var filteredData = new List<QuestionResponse>();

            foreach (var question in response.Data)
            {
                var questionUser = result.Data.FirstOrDefault(q => q.Id == question.Id)?.CreatedByUser;
                if (questionUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(questionUser);
                    question.CreatedByRole = _permissionService.GetHighestPriorityRole(roles);

                    if (userRole == nameof(Role.Teacher) || userRole == nameof(Role.ContentModerator))
                    {
                        if (lesson.Visibility == LessonMaterialVisibility.School)
                        {
                            filteredData.Add(question);
                        }
                        else
                        {
                            if (roles.Contains(nameof(Role.Student)) || question.CreatedByUserId == request.CurrentUserId)
                            {
                                filteredData.Add(question);
                            }
                        }
                    }
                    else
                    {
                        filteredData.Add(question);
                    }
                }
            }

            return new Pagination<QuestionResponse>(
                response.PageIndex,
                response.PageSize,
                totalCount,
                filteredData
            );
        }

        #region Validation User Access Material

        private async Task ValidateUserAccessToMaterial(Guid userId, Guid lessonMaterialId, string userRole, LessonMaterial lessonMaterial)
        {
            var studentClassCustomRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

            switch (userRole)
            {
                case nameof(Role.Student):
                    await ValidateStudentAccess(userId, lessonMaterialId, studentClassCustomRepo);
                    break;

                case nameof(Role.Teacher):
                    await ValidateTeacherAccess(userId, lessonMaterialId, studentClassCustomRepo, lessonMaterial);
                    break;

                case nameof(Role.ContentModerator):
                case nameof(Role.SchoolAdmin):
                case nameof(Role.SystemAdmin):
                    break;

                default:
                    throw new AppException(CustomCode.InsufficientPermission);
            }
        }

        #endregion

        #region Validation Student Access

        private async Task ValidateStudentAccess(Guid userId, Guid lessonMaterialId, IStudentClassRepository repo)
        {
            var hasAccess = await repo.HasAccessToMaterialAsync(userId, lessonMaterialId);

            if (!hasAccess)
            {
                var isEnrolledInAnyClass = await repo.IsEnrolledInAnyClassAsync(userId);

                if (!isEnrolledInAnyClass)
                {
                    throw new AppException(CustomCode.StudentNotEnrolledInAnyClassForQuestions);
                }

                var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, Guid>();
                var materialExistsInAnyFolder = await folderLessonMaterialRepo.ExistsAsync(flm => flm.LessonMaterialId == lessonMaterialId);

                if (!materialExistsInAnyFolder)
                {
                    throw new AppException(CustomCode.MaterialNotAccessibleToStudent);

                }
                else
                {
                    throw new AppException(CustomCode.StudentNotEnrolledInClassWithMaterial);
                }
            }
        }

        #endregion

        #region Validation Teacher Access

        private static async Task ValidateTeacherAccess(Guid teacherId, Guid lessonMaterialId, IStudentClassRepository repo, LessonMaterial lessonMaterial)
        {
            if (lessonMaterial.CreatedByUserId == teacherId)
            {
                return;
            }

            var hasAccess = await repo.TeacherHasAccessToMaterialAsync(teacherId, lessonMaterialId);

            if (!hasAccess)
            {
                var hasActiveClass = await repo.TeacherHasActiveClassAsync(teacherId);

                if (!hasActiveClass)
                {
                    throw new AppException(CustomCode.TeacherMustHaveActiveClass);
                }

                throw new AppException(CustomCode.TeacherNotHaveAccessToMaterial);
            }
        }

        #endregion

    }
}