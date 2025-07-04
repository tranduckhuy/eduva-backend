using Eduva.Application.Features.LessonMaterials.Queries.Extensions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialsValidator : AbstractValidator<GetLessonMaterialsQuery>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetLessonMaterialsValidator(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;

            // Basic parameter validation
            RuleFor(x => x.LessonMaterialSpecParam.SearchTerm)
                .MaximumLength(255).WithMessage("Search term must not exceed 255 characters.");

            RuleFor(x => x.LessonMaterialSpecParam.Tag)
                .MaximumLength(100).WithMessage("Tag must not exceed 100 characters.");

            // Main authorization validation
            RuleFor(x => x)
                .MustAsync(ValidateAccess)
                .WithMessage("Access denied. You don't have permission to access these lesson materials.");

            // Basic existence validation
            RuleFor(x => x.SchoolId)
                .MustAsync(SchoolExists)
                .WithMessage("The specified school does not exist.")
                .When(x => x.SchoolId.HasValue);

            RuleFor(x => x.LessonMaterialSpecParam.ClassId)
                .MustAsync(ClassExists)
                .WithMessage("The specified class does not exist.")
                .When(x => x.LessonMaterialSpecParam.ClassId.HasValue);

            RuleFor(x => x.LessonMaterialSpecParam.FolderId)
                .MustAsync(FolderExists)
                .WithMessage("The specified folder does not exist.")
                .When(x => x.LessonMaterialSpecParam.FolderId.HasValue);
        }

        private async Task<bool> ValidateAccess(GetLessonMaterialsQuery query, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(query.UserId.ToString());
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            var param = query.LessonMaterialSpecParam;

            // SystemAdmin can access everything
            if (roles.HasSystemAdminRole()) return true;

            // Get user's school
            var userSchoolId = user.SchoolId;

            if (userSchoolId == null)
            {
                return false;
            }

            // SchoolAdmin/ContentModerator: bypass if same school
            if (roles.HasSchoolManagementRoles())
            {
                return !query.SchoolId.HasValue || query.SchoolId == userSchoolId;
            }

            // Teacher: same school for public, same class for all
            if (roles.HasTeacherRole())
            {
                // If specific class requested, must be their class
                if (param.ClassId.HasValue)
                {
                    var classroom = await _unitOfWork.GetRepository<Classroom, Guid>().GetByIdAsync(param.ClassId.Value);
                    return classroom?.TeacherId == query.UserId;
                }
                // For general access, same school only
                return !query.SchoolId.HasValue || query.SchoolId == userSchoolId;
            }

            // Student: only if enrolled in the specific class
            if (roles.HasStudentRole())
            {
                // Must have classId for students
                if (!param.ClassId.HasValue) return false;

                return await IsStudentEnrolledInClass(query.UserId, param.ClassId.Value);
            }

            return false;
        }

        private async Task<bool> IsStudentEnrolledInClass(Guid studentId, Guid classId)
        {
            var studentClassRepository = _unitOfWork.GetRepository<StudentClass, Guid>();
            var enrollment = await studentClassRepository.FirstOrDefaultAsync(
                sc => sc.StudentId == studentId && sc.ClassId == classId);
            return enrollment != null;
        }

        private async Task<bool> SchoolExists(int? schoolId, CancellationToken cancellationToken)
        {
            if (!schoolId.HasValue) return true;

            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            return await schoolRepository.ExistsAsync(schoolId.Value);
        }

        private async Task<bool> ClassExists(Guid? classId, CancellationToken cancellationToken)
        {
            if (!classId.HasValue) return true;

            var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
            return await classRepository.ExistsAsync(classId.Value);
        }

        private async Task<bool> FolderExists(Guid? folderId, CancellationToken cancellationToken)
        {
            if (!folderId.HasValue) return true;

            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
            return await folderRepository.ExistsAsync(folderId.Value);
        }
    }
}
