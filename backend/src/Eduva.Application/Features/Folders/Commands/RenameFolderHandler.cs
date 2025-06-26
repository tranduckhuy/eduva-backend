using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Folders.Commands
{
    public class RenameFolderHandler : IRequestHandler<RenameFolderCommand, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RenameFolderHandler> _logger;

        public RenameFolderHandler(IUnitOfWork unitOfWork, ILogger<RenameFolderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<FolderResponse> Handle(RenameFolderCommand request, CancellationToken cancellationToken)
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

            // Check for duplicate folder name within the same scope
            bool folderExists = await folderRepository.ExistsAsync(f =>
                f.Id != request.Id &&
                f.Name == request.Name &&
                (folder.OwnerType == OwnerType.Personal && f.UserId == folder.UserId ||
                 folder.OwnerType == OwnerType.Class && f.ClassId == folder.ClassId) &&
                f.Status == EntityStatus.Active);

            if (folderExists)
            {
                throw new AppException(CustomCode.FolderNameAlreadyExists);
            }

            // Update folder name
            folder.Name = request.Name;
            folder.LastModifiedAt = DateTimeOffset.UtcNow;

            try
            {
                folderRepository.Update(folder);
                await _unitOfWork.CommitAsync();

                return AppMapper.Mapper.Map<FolderResponse>(folder);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Failed to rename folder: {Message}", ex.Message);
                throw new AppException(CustomCode.FolderUpdateFailed);
            }
        }

        private async Task<bool> HasPermissionToUpdateFolder(Folder folder, Guid userId)
        {
            // Personal folder - only the owner can update
            if (folder.OwnerType == OwnerType.Personal)
            {
                return folder.UserId == userId;
            }

            // Class folder - check if user is teacher of the class or admin
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

                // Check if user is an admin
                var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var user = await userRepository.GetByIdAsync(userId);

                if (user != null && user.SchoolId == classroom.SchoolId)
                {
                    return true; // School admin
                }
            }

            return false;
        }
    }
}
