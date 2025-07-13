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
    public class UpdateFolderOrderHandler : IRequestHandler<UpdateFolderOrderCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateFolderOrderHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public UpdateFolderOrderHandler(IUnitOfWork unitOfWork, ILogger<UpdateFolderOrderHandler> logger, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(UpdateFolderOrderCommand request, CancellationToken cancellationToken)
        {
            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();

            // Retrieve folder
            var folder = await folderRepository.GetByIdAsync(request.Id);
            if (folder == null || folder.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.FolderNotFound);
            }

            // Verify ownership and permissions
            bool hasPermission = await HasPermissionToUpdateFolder(folder, request.CurrentUserId);
            if (!hasPermission)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            // Save the original order for comparison
            int originalOrder = folder.Order;

            // If order is the same, no need to update
            if (originalOrder == request.Order)
            {
                return Unit.Value;
            }

            try
            {
                // Get all related folders in the same scope
                var allFolders = await folderRepository.GetAllAsync();
                var relatedFolders = allFolders
                    .Where(f => f.Id != folder.Id &&
                                f.Status == EntityStatus.Active &&
                                ((folder.OwnerType == OwnerType.Personal &&
                                 f.OwnerType == OwnerType.Personal &&
                                 f.UserId == folder.UserId) ||
                                (folder.OwnerType == OwnerType.Class &&
                                 f.OwnerType == OwnerType.Class &&
                                 f.ClassId == folder.ClassId)))
                    .OrderBy(f => f.Order)
                    .ToList();

                // Update the target folder's order
                folder.Order = request.Order;
                folder.LastModifiedAt = DateTimeOffset.UtcNow;

                // Reorder other folders to avoid duplicates
                if (request.Order < originalOrder)
                {
                    // Shift all folders that are between new position and old position (inclusive-exclusive)
                    foreach (var relatedFolder in relatedFolders)
                    {
                        if (relatedFolder.Order >= request.Order && relatedFolder.Order < originalOrder)
                        {
                            relatedFolder.Order += 1;
                            relatedFolder.LastModifiedAt = DateTimeOffset.UtcNow;
                            folderRepository.Update(relatedFolder);
                        }
                    }
                }
                else
                {
                    // Shift all folders that are between old position and new position (exclusive-inclusive)
                    foreach (var relatedFolder in relatedFolders)
                    {
                        if (relatedFolder.Order > originalOrder && relatedFolder.Order <= request.Order)
                        {
                            relatedFolder.Order -= 1;
                            relatedFolder.LastModifiedAt = DateTimeOffset.UtcNow;
                            folderRepository.Update(relatedFolder);
                        }
                    }
                }

                // Update the target folder
                folderRepository.Update(folder);
                await _unitOfWork.CommitAsync();

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update folder order: {Message}", ex.Message);
                throw new AppException(CustomCode.FolderUpdateFailed);
            }
        }

        private async Task<bool> HasPermissionToUpdateFolder(Folder folder, Guid userId)
        {
            if (folder.OwnerType == OwnerType.Personal)
            {
                return folder.UserId == userId;
            }

            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);

                if (classroom == null)
                {
                    return false;
                }

                var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var user = await userRepository.GetByIdAsync(userId);

                if (user == null)
                    return false;

                var roles = await _userManager.GetRolesAsync(user);

                if ((roles.Contains(nameof(Role.Teacher)) || roles.Contains(nameof(Role.ContentModerator))) && classroom.TeacherId == userId)
                {
                    return true;
                }

                if (roles.Contains(nameof(Role.SchoolAdmin)) && user.SchoolId == classroom.SchoolId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
