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

            var folder = await folderRepository.GetByIdAsync(request.Id);
            if (folder == null)
                throw new AppException(CustomCode.FolderNotFound);

            if (folder.OwnerType != OwnerType.Personal)
                throw new AppException(CustomCode.FolderMustBePersonal);

            if (folder.Status == EntityStatus.Archived)
                throw new AppException(CustomCode.FolderAlreadyArchived);

            if (folder.Status == EntityStatus.Deleted)
                throw new AppException(CustomCode.FolderDeleteFailed);

            bool hasPermission = await HasPermissionToUpdateFolder(folder, request.CurrentUserId);
            if (!hasPermission)
                throw new AppException(CustomCode.Forbidden);

            try
            {
                folder.Status = EntityStatus.Archived;
                folder.LastModifiedAt = DateTimeOffset.UtcNow;
                folderRepository.Update(folder);

                var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
                var lessonMaterialRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();

                var allFolderLessonMaterials = await folderLessonMaterialRepo.GetAllAsync();
                var lessonMaterialIds = allFolderLessonMaterials
                    .Where(flm => flm.FolderId == folder.Id)
                    .Select(flm => flm.LessonMaterialId)
                    .ToList();

                var allLessonMaterials = await lessonMaterialRepo.GetAllAsync();
                var materialsToArchive = allLessonMaterials
                    .Where(lm => lessonMaterialIds.Contains(lm.Id) && lm.Status != EntityStatus.Deleted)
                    .ToList();

                foreach (var lessonMaterial in materialsToArchive)
                {
                    lessonMaterial.Status = EntityStatus.Deleted;
                    lessonMaterialRepo.Update(lessonMaterial);
                }

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

                if ((userRoles.Contains(Role.Teacher.ToString()) || userRoles.Contains(Role.ContentModerator.ToString()))
                    && classroom.TeacherId == userId)
                {
                    return true;
                }

                if (userRoles.Contains(Role.SchoolAdmin.ToString()) && user.SchoolId == classroom.SchoolId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
