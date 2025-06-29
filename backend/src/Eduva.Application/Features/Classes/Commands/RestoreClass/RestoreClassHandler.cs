using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands.RestoreClass
{
    public class RestoreClassHandler : IRequestHandler<RestoreClassCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public RestoreClassHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(RestoreClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Check if the class is not archived
            if (classroom.Status != EntityStatus.Archived)
            {
                throw new AppException(CustomCode.ClassNotArchived);
            }

            // Get the current user
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var currentUser = await userRepository.GetByIdAsync(request.TeacherId)
                ?? throw new AppException(CustomCode.UserNotExists);

            // Check user roles
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool isTeacherOfClass = classroom.TeacherId == request.TeacherId;
            bool isAdmin = userRoles.Contains(nameof(Role.SystemAdmin)) || userRoles.Contains(nameof(Role.SchoolAdmin));

            // Only allow the teacher of the class or admins to restore the class
            if (!isTeacherOfClass && !isAdmin)
            {
                throw new AppException(CustomCode.NotTeacherOfClass);
            }
            try
            {
                // Set the class status to active
                classroom.Status = EntityStatus.Active;
                classroom.LastModifiedAt = DateTimeOffset.UtcNow;

                classroomRepository.Update(classroom);
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.ClassRestoreFailed);
            }
        }
    }
}
