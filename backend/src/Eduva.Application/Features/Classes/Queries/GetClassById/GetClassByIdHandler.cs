using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetClassById
{
    public class GetClassByIdHandler : IRequestHandler<GetClassByIdQuery, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetClassByIdHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ClassResponse> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            var classroom = await classroomRepository.GetByIdAsync(request.Id);

            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            // Check if the user has access to this class (teacher, school admin, or student enrolled)
            bool hasAccess = await HasAccessToClass(classroom, request.UserId);
            if (!hasAccess)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            var response = AppMapper.Mapper.Map<ClassResponse>(classroom);
            response.TeacherName = classroom.Teacher?.FullName ?? string.Empty;
            response.SchoolName = classroom.School?.Name ?? string.Empty;

            return response;
        }

        private async Task<bool> HasAccessToClass(Classroom classroom, Guid userId)
        {
            // Teacher of the class
            if (classroom.TeacherId == userId)
                return true;
            // Check if user is a student in this class
            var studentClassRepository = _unitOfWork.GetRepository<StudentClass, Guid>();
            bool isStudentInClass = await studentClassRepository.ExistsAsync(sc =>
                sc.ClassId == classroom.Id && sc.StudentId == userId);

            if (isStudentInClass)
                return true;
            // Check if user is school admin
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(userId);

            // If user is from the same school as the class, they have access
            if (user != null && user.SchoolId == classroom.SchoolId)
                return true;

            return false;
        }
    }
}
