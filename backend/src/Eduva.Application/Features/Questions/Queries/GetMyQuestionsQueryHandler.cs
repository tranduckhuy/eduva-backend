using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Questions.Queries
{
    public class GetMyQuestionsQueryHandler
        : IRequestHandler<GetMyQuestionsQuery, Pagination<QuestionResponse>>
    {
        private readonly ILessonMaterialQuestionRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public GetMyQuestionsQueryHandler(ILessonMaterialQuestionRepository repository, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<QuestionResponse>> Handle(GetMyQuestionsQuery request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.UserId) ?? throw new AppException(CustomCode.UserNotFound);

            if (!user.SchoolId.HasValue)
            {
                throw new AppException(CustomCode.UserNotPartOfSchool);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = GetHighestPriorityRole(userRoles);

            await ValidateUserEligibility(request.UserId, user.SchoolId.Value, userRole);

            var spec = new MyQuestionsSpecification(request.Param, request.UserId, user.SchoolId);
            var result = await _repository.GetWithSpecAsync(spec);
            var response = AppMapper.Mapper.Map<Pagination<QuestionResponse>>(result);

            foreach (var question in response.Data)
            {
                question.CreatedByRole = userRole;
            }

            return response;
        }

        #region Validation Methods

        private async Task ValidateUserEligibility(Guid userId, int schoolId, string userRole)
        {
            var studentClassCustomRepo = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

            switch (userRole)
            {
                case nameof(Role.Student):
                    await ValidateStudentEligibility(userId, schoolId, studentClassCustomRepo);
                    break;

                case nameof(Role.Teacher):
                case nameof(Role.ContentModerator):
                    await ValidateTeacherEligibility(userId, schoolId, studentClassCustomRepo);
                    break;

                case nameof(Role.SchoolAdmin):
                case nameof(Role.SystemAdmin):
                    break;

                default:
                    throw new AppException(CustomCode.InsufficientPermission);
            }
        }

        #endregion

        #region Validation Student Eligibility

        private static async Task ValidateStudentEligibility(Guid userId, int schoolId, IStudentClassRepository repo)
        {
            var isEnrolledInAnyClass = await repo.IsEnrolledInAnyClassAsync(userId);
            if (!isEnrolledInAnyClass)
            {
                throw new AppException(CustomCode.StudentNotEnrolledInAnyClassForQuestions);
            }

            var hasValidClassInSchool = await repo.HasValidClassInSchoolAsync(userId, schoolId);
            if (!hasValidClassInSchool)
            {
                throw new AppException(CustomCode.StudentNotInSchoolClass);
            }
        }

        #endregion

        #region Validation Teacher Eligibility

        private static async Task ValidateTeacherEligibility(Guid teacherId, int schoolId, IStudentClassRepository repo)
        {
            var hasActiveClass = await repo.TeacherHasActiveClassAsync(teacherId);
            if (!hasActiveClass)
            {
                throw new AppException(CustomCode.TeacherMustHaveActiveClass);
            }

            var hasValidClassInSchool = await repo.TeacherHasValidClassInSchoolAsync(teacherId, schoolId);
            if (!hasValidClassInSchool)
            {
                throw new AppException(CustomCode.TeacherClassNotInOwnSchool);
            }
        }

        #endregion

        #region Role Priority Logic

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

        #endregion

    }
}