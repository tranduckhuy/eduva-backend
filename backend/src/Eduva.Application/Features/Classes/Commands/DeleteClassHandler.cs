using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands
{
    public class DeleteClassHandler : IRequestHandler<DeleteClassCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteClassHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

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

            // Only allow the teacher of the class or admins to delete the class
            if (!isTeacherOfClass && !isAdmin)
            {
                throw new AppException(CustomCode.NotTeacherOfClass);
            }

            classroom.Status = EntityStatus.Archived;
            classroom.LastModifiedAt = DateTimeOffset.UtcNow;
            classroomRepository.Update(classroom);

            try
            {
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.ClassArchiveFailed);
            }
        }
    }
}
