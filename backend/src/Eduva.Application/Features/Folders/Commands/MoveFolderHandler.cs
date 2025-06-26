using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.Folders.Commands
{
    public class MoveFolderHandler : IRequestHandler<MoveFolderCommand, FolderResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MoveFolderHandler> _logger;
        private readonly IFolderRepository _folderRepository;

        public MoveFolderHandler(IUnitOfWork unitOfWork, ILogger<MoveFolderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _folderRepository = unitOfWork.GetCustomRepository<IFolderRepository>();
        }

        public async Task<FolderResponse> Handle(MoveFolderCommand request, CancellationToken cancellationToken)
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

            // Automatically determine OwnerType based on provided ClassId
            if (request.ClassId.HasValue)
            {
                request.OwnerType = OwnerType.Class;
                request.UserId = null;

                // Verify class exists and user has permission
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(request.ClassId.Value);

                if (classroom == null)
                {
                    throw new AppException(CustomCode.ClassNotFound);
                }

                // Check if user is teacher of the target class
                if (classroom.TeacherId != request.CurrentUserId)
                {
                    // Check if user is an admin of the school
                    var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                    var user = await userRepository.GetByIdAsync(request.CurrentUserId);

                    if (user == null || user.SchoolId != classroom.SchoolId)
                    {
                        throw new AppException(CustomCode.NotTeacherOfClass);
                    }
                }
            }
            else
            {
                // If no ClassId provided, default to personal folder
                request.OwnerType = OwnerType.Personal;
                request.UserId = request.CurrentUserId;
                request.ClassId = null;
            }

            // Check for duplicate folder name within the target scope
            bool folderExists = await folderRepository.ExistsAsync(f =>
                f.Id != request.Id &&
                f.Name == folder.Name &&
                (request.OwnerType == OwnerType.Personal && f.UserId == request.UserId ||
                 request.OwnerType == OwnerType.Class && f.ClassId == request.ClassId) &&
                f.Status == EntityStatus.Active);

            if (folderExists)
            {
                throw new AppException(CustomCode.FolderNameAlreadyExists);
            }

            // Get the next order number in the target location
            int nextOrder = await _folderRepository.GetMaxOrderAsync(request.UserId, request.ClassId) + 1;

            // Update folder properties
            folder.OwnerType = request.OwnerType;
            folder.UserId = request.UserId;
            folder.ClassId = request.ClassId;
            folder.Order = nextOrder;
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
                _logger.LogError(ex, "Failed to move folder: {Message}", ex.Message);
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
