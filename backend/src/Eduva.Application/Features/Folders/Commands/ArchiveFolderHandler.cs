using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Folders.Commands
{
    public class ArchiveFolderHandler : IRequestHandler<ArchiveFolderCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ArchiveFolderHandler(
            IUnitOfWork unitOfWork,
            ILogger<ArchiveFolderHandler> logger,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(ArchiveFolderCommand request, CancellationToken cancellationToken)
        {
            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();

            // Retrieve folder
            var folder = await folderRepository.GetByIdAsync(request.Id);
            if (folder == null)
            {
                throw new AppException(CustomCode.FolderNotFound);
            }

            // Check if folder is already archived
            if (folder.Status == EntityStatus.Archived)
            {
                throw new AppException(CustomCode.FolderAlreadyArchived);
            }

            if (folder.Status == EntityStatus.Deleted)
            {
                throw new AppException(CustomCode.FolderDeleteFailed);
            }

            // Verify ownership and permissions
            bool hasPermission = await HasPermissionToUpdateFolder(folder, request.CurrentUserId);
            if (!hasPermission)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            try
            {
                // Update folder status to Archived
                folder.Status = EntityStatus.Archived;
                folder.LastModifiedAt = DateTimeOffset.UtcNow;

                folderRepository.Update(folder);
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.FolderArchiveFailed);
            }
        }
        private async Task<bool> HasPermissionToUpdateFolder(Folder folder, Guid userId)
        {
            // Get user information
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return false;
            }

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // System Admin can archive any folder
            if (userRoles.Contains(Role.SystemAdmin.ToString()))
            {
                return true;
            }

            // Personal folder - only the owner can update
            if (folder.OwnerType == OwnerType.Personal)
            {
                return folder.UserId == userId;
            }

            // Class folder - check if user is teacher of the class or school admin
            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);

                if (classroom == null)
                {
                    return false;
                }

                // Teacher of the class
                if (classroom.TeacherId == userId)
                {
                    return true;
                }

                // School Admin can archive any folder from their school
                if (userRoles.Contains(Role.SchoolAdmin.ToString()) && user.SchoolId == classroom.SchoolId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
