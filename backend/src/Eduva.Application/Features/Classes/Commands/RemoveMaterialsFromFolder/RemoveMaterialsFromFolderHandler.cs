using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder
{
    public class RemoveMaterialsFromFolderHandler : IRequestHandler<RemoveMaterialsFromFolderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public RemoveMaterialsFromFolderHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> Handle(RemoveMaterialsFromFolderCommand request, CancellationToken cancellationToken)
        {
            // Validate folder exists
            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
            var folder = await folderRepository.GetByIdAsync(request.FolderId);
            if (folder == null)
            {
                throw new AppException(CustomCode.FolderNotFound);
            }

            // Validate folder belongs to the specified class
            if (folder.OwnerType != OwnerType.Class || folder.ClassId != request.ClassId)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            // Check access permission
            await CheckFolderAccessAsync(folder, request.CurrentUserId);

            var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
            var allFolderMaterials = await folderLessonMaterialRepo.GetAllAsync();
            var folderMaterials = allFolderMaterials
                .Where(flm => flm.FolderId == request.FolderId && request.MaterialIds.Contains(flm.LessonMaterialId))
                .ToList();

            if (!folderMaterials.Any())
            {
                throw new AppException(CustomCode.LessonMaterialNotFoundInFolder);
            }

            // Remove materials from folder
            foreach (var folderMaterial in folderMaterials)
            {
                folderLessonMaterialRepo.Remove(folderMaterial);
            }

            // Save changes
            await _unitOfWork.CommitAsync();

            return true;
        }

        private async Task CheckFolderAccessAsync(Folder folder, Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new AppException(CustomCode.UserNotExists);
            }

            var roles = await _userManager.GetRolesAsync(user);

            // System admin can access any folder
            if (roles.Contains(nameof(Role.SystemAdmin)))
            {
                return;
            }

            // Check if folder belongs to class
            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);

                if (classroom == null)
                {
                    throw new AppException(CustomCode.ClassNotFound);
                }

                // Teacher of the class
                if (classroom.TeacherId == userId)
                {
                    return;
                }

                // School admin of the same school
                if (roles.Contains(nameof(Role.SchoolAdmin)) && classroom.SchoolId == user.SchoolId)
                {
                    return;
                }
            }

            throw new AppException(CustomCode.Unauthorized);
        }
    }
}