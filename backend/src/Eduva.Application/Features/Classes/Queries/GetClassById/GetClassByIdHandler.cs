using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Queries.GetClassById
{
    public class GetClassByIdHandler : IRequestHandler<GetClassByIdQuery, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetClassByIdHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<ClassResponse> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
            var schoolRepository = _unitOfWork.GetCustomRepository<ISchoolRepository>();

            var classroom = await classroomRepository.GetByIdAsync(request.Id);
            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            var teacher = await userRepository.GetByIdAsync(classroom.TeacherId);
            var school = await schoolRepository.GetByIdAsync(classroom.SchoolId);

            bool hasAccess = await HasAccessToClass(classroom, request.UserId);
            if (!hasAccess)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            var response = AppMapper<AppMappingProfile>.Mapper.Map<ClassResponse>(classroom);
            response.TeacherName = teacher?.FullName ?? string.Empty;
            response.SchoolName = school?.Name ?? string.Empty;
            response.TeacherAvatarUrl = teacher?.AvatarUrl ?? string.Empty;

            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
            var allFolders = await folderRepository.GetAllAsync();

            var folders = allFolders.Where(f =>
                f.OwnerType == OwnerType.Class &&
                f.ClassId.HasValue &&
                f.ClassId.Value == request.Id
            ).ToList();

            if (folders.Count > 0)
            {
                var folderIds = folders.Select(f => f.Id).ToList();
                var lessonMaterialRepo = _unitOfWork.GetCustomRepository<ILessonMaterialRepository>();
                response.CountLessonMaterial = await lessonMaterialRepo.CountApprovedMaterialsInFoldersAsync(folderIds, cancellationToken);
            }
            else
            {
                response.CountLessonMaterial = 0;
            }
            return response;
        }

        private async Task<bool> HasAccessToClass(Classroom classroom, Guid userId)
        {
            if (classroom.TeacherId == userId)
                return true;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            var roles = await _userManager.GetRolesAsync(user);

            bool isStudent = roles.Contains(nameof(Role.Student));
            if (isStudent)
            {
                var studentClassRepository = _unitOfWork.GetRepository<StudentClass, Guid>();
                bool isStudentInClass = await studentClassRepository.ExistsAsync(sc =>
                    sc.ClassId == classroom.Id && sc.StudentId == userId);

                return isStudentInClass;
            }

            bool isTeacher = roles.Contains(nameof(Role.Teacher));
            if (isTeacher)
            {
                return false;
            }

            bool isSchoolAdmin = roles.Contains(nameof(Role.SchoolAdmin));
            if (isSchoolAdmin && user.SchoolId == classroom.SchoolId)
                return true;

            bool isSystemAdmin = roles.Contains(nameof(Role.SystemAdmin));
            if (isSystemAdmin)
                return true;

            return false;
        }
    }
}
