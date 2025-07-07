using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands.RemoveStudentsFromClass
{
    public class RemoveStudentsFromClassHandler : IRequestHandler<RemoveStudentsFromClassCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RemoveStudentsFromClassHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(RemoveStudentsFromClassCommand request, CancellationToken cancellationToken)
        {
            // Validate class exists
            var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
            var classroom = await classRepository.GetByIdAsync(request.ClassId);
            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            // Check permission
            var hasPermission = await CheckPermission(request, classroom);
            if (!hasPermission)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            // Get student classes to remove
            var studentClassRepository = _unitOfWork.GetRepository<StudentClass, Guid>();

            var allStudentClasses = await studentClassRepository.GetAllAsync();
            var studentClasses = allStudentClasses
                .Where(sc => sc.ClassId == request.ClassId && request.StudentIds.Contains(sc.StudentId))
                .ToList();

            if (studentClasses.Count == 0)
            {
                throw new AppException(CustomCode.StudentNotFoundInClass);
            }

            foreach (var studentClass in studentClasses)
            {
                studentClassRepository.Remove(studentClass);
            }

            // Save changes
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }

        private async Task<bool> CheckPermission(RemoveStudentsFromClassCommand request, Classroom classroom)
        {
            // System admin can do anything
            if (request.IsSystemAdmin)
            {
                return true;
            }

            // Teacher can only remove students from their own classes
            if (request.IsTeacher)
            {
                return classroom.TeacherId == request.RequestUserId;
            }

            // School admin can only remove students from classes in their school
            if (request.IsSchoolAdmin)
            {
                var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var user = await userRepository.GetByIdAsync(request.RequestUserId);

                if (user == null)
                {
                    return false;
                }

                return user.SchoolId == classroom.SchoolId;
            }

            return false;
        }
    }
}