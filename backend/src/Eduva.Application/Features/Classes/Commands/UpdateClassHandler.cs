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

namespace Eduva.Application.Features.Classes.Commands
{
    public class UpdateClassHandler : IRequestHandler<UpdateClassCommand, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateClassHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<ClassResponse> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Get the current user
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var currentUser = await userRepository.GetByIdAsync(request.TeacherId)
                ?? throw new AppException(CustomCode.UserNotExists);

            // Check user roles
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool isTeacherOfClass = classroom.TeacherId == request.TeacherId;
            bool isAdmin = userRoles.Contains(nameof(Role.SystemAdmin)) || userRoles.Contains(nameof(Role.SchoolAdmin));

            // Only allow the teacher of the class or admins to update the class
            if (!isTeacherOfClass && !isAdmin)
            {
                throw new AppException(CustomCode.NotTeacherOfClass);
            }

            // Check if the school exists
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(classroom.SchoolId)
                ?? throw new AppException(CustomCode.SchoolNotFound);

            // Check if the new class name already exists for the same teacher (excluding current class)
            if (classroom.Name.ToLower() != request.Name.ToLower())
            {
                bool classExists = await classroomRepository.ExistsAsync(c =>
                    c.Id != request.Id &&
                    c.TeacherId == classroom.TeacherId &&  // Only check within the scope of the same teacher
                    c.Name.ToLower() == request.Name.ToLower());
                if (classExists)
                {
                    throw new AppException(CustomCode.ClassNameAlreadyExistsForTeacher);
                }
            }

            // Update only the fields that should be updated
            classroom.Name = request.Name;
            classroom.Status = request.Status;
            classroom.LastModifiedAt = DateTimeOffset.UtcNow;

            classroomRepository.Update(classroom);

            try
            {
                await _unitOfWork.CommitAsync();

                // Map the response with teacher and school information
                var response = AppMapper.Mapper.Map<ClassResponse>(classroom);
                response.TeacherName = classroom.Teacher?.FullName ?? string.Empty;
                response.SchoolName = school.Name;

                return response;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.ClassUpdateFailed);
            }
        }
    }
}
