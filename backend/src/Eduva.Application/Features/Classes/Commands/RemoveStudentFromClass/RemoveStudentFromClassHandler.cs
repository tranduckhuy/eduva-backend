using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands.RemoveStudentFromClass
{
    public class RemoveStudentFromClassHandler : IRequestHandler<RemoveStudentFromClassCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        public RemoveStudentFromClassHandler(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(RemoveStudentFromClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.ClassId)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Check if the classroom is archived
            if (classroom.Status == EntityStatus.Archived)
            {
                throw new AppException(CustomCode.ClassNotActive);
            }

            // Check if the student is enrolled in the class
            var enrollment = await studentClassRepository.GetStudentClassAsync(request.StudentId, request.ClassId)
                ?? throw new AppException(CustomCode.StudentNotEnrolled);

            // Verify the authorization based on role
            if (request.IsTeacher)
            {
                // Teachers can only remove students from their own classes
                if (classroom.TeacherId != request.RequestUserId)
                {
                    throw new AppException(CustomCode.NotTeacherOfClass);
                }
            }
            else if (request.IsSchoolAdmin)
            {
                // School admins can only remove students from classes in their school
                var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var schoolAdmin = await userRepository.GetByIdAsync(request.RequestUserId)
                    ?? throw new AppException(CustomCode.UserNotExists);

                if (classroom.SchoolId != schoolAdmin.SchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
            }
            else if (!request.IsSystemAdmin)
            {
                // If not a teacher, school admin, or system admin, deny access
                throw new AppException(CustomCode.Forbidden);
            }

            try
            {
                // Remove the student from the class
                studentClassRepository.Remove(enrollment);
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.StudentRemovalFailed);
            }
        }
    }
}
