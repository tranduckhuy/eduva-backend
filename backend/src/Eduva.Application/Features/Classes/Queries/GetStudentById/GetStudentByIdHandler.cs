using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetStudentById
{
    public class GetStudentByIdHandler : IRequestHandler<GetStudentByIdQuery, StudentClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetStudentByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<StudentClassResponse> Handle(GetStudentByIdQuery request, CancellationToken cancellationToken)
        {
            var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            var schoolRepository = _unitOfWork.GetCustomRepository<ISchoolRepository>();

            var studentClass = await studentClassRepository.GetStudentClassByIdAsync(request.Id);
            if (studentClass == null)
            {
                throw new AppException(CustomCode.UserIdNotFound);
            }
            var student = await userRepository.GetByIdAsync(studentClass.StudentId);
            if (student == null)
            {
                throw new AppException(CustomCode.UserNotFound);
            }
            var classroom = await classroomRepository.GetByIdAsync(studentClass.ClassId);
            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }
            var school = await schoolRepository.GetByIdAsync(classroom.SchoolId);
            bool hasAccess = await HasAccessToStudentClass(studentClass, request.UserId);
            if (!hasAccess)
            {
                throw new AppException(CustomCode.Unauthorized);
            }
            var response = AppMapper<AppMappingProfile>.Mapper.Map<StudentClassResponse>(studentClass);
            response.StudentName = student.FullName ?? string.Empty;
            response.ClassName = classroom.Name;
            response.TeacherName = (await userRepository.GetByIdAsync(classroom.TeacherId))?.FullName ?? string.Empty;
            response.SchoolName = school?.Name ?? string.Empty;
            response.StudentAvatarUrl = student.AvatarUrl;
            response.TeacherAvatarUrl = (await userRepository.GetByIdAsync(classroom.TeacherId))?.AvatarUrl;
            return response;
        }

        private async Task<bool> HasAccessToStudentClass(StudentClass studentClass, Guid userId)
        {
            if (studentClass.StudentId == userId)
                return true;

            if (studentClass.Class != null && studentClass.Class.TeacherId == userId)
                return true;

            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(userId);

            if (user != null && studentClass.Class != null)
            {
                var schoolRepository = _unitOfWork.GetCustomRepository<ISchoolRepository>();
                var school = await schoolRepository.GetByIdAsync(studentClass.Class.SchoolId);
                if (school != null && user.SchoolId == school.Id)
                    return true;
            }
            return false;
        }
    }
}
