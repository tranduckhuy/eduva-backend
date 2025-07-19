using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Commands.RestoreLessonMaterial
{
    public class RestoreLessonMaterialHandler : IRequestHandler<RestoreLessonMaterialCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public RestoreLessonMaterialHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> Handle(RestoreLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var folderRepo = _unitOfWork.GetRepository<Folder, Guid>();
            var lessonMaterialRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();

            var user = await userRepo.GetByIdAsync(request.CurrentUserId) ?? throw new AppException(CustomCode.UserNotExists);

            var folder = await folderRepo.GetByIdAsync(request.PersonalFolderId) ?? throw new AppException(CustomCode.FolderNotFound);

            if (folder.OwnerType != OwnerType.Personal || folder.Status != EntityStatus.Active)
                throw new AppException(CustomCode.InvalidPersonalFolder);

            if (!HasPermissionToUseFolder(folder, user))
                throw new AppException(CustomCode.Forbidden);

            var lessonMaterials = await lessonMaterialRepo.GetAllAsync();
            var toRestore = lessonMaterials
                .Where(lm => request.LessonMaterialIds.Contains(lm.Id) && lm.Status == EntityStatus.Deleted)
                .ToList();

            if (toRestore.Count == 0)
                throw new AppException(CustomCode.LessonMaterialNotFound);

            try
            {
                foreach (var lm in toRestore)
                {
                    if (!await HasPermissionToRestoreLessonMaterial(lm, user))
                        throw new AppException(CustomCode.Forbidden);

                    lm.Status = EntityStatus.Active;
                    lessonMaterialRepo.Update(lm);

                    var exists = (await folderLessonMaterialRepo.GetAllAsync())
                        .Any(flm => flm.FolderId == folder.Id && flm.LessonMaterialId == lm.Id);
                    if (!exists)
                    {
                        await folderLessonMaterialRepo.AddAsync(new FolderLessonMaterial
                        {
                            FolderId = folder.Id,
                            LessonMaterialId = lm.Id
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex) when (ex is not AppException)
            {
                throw new AppException(CustomCode.LessonMaterialRestoreFailed);
            }
        }

        private static bool HasPermissionToUseFolder(Folder folder, ApplicationUser user)
        {
            if (folder.OwnerType == OwnerType.Personal)
                return folder.UserId == user.Id;

            return false;
        }

        private async Task<bool> HasPermissionToRestoreLessonMaterial(LessonMaterial lm, ApplicationUser user)
        {
            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Contains(Role.Teacher.ToString()) || userRoles.Contains(Role.ContentModerator.ToString()) || userRoles.Contains(Role.SchoolAdmin.ToString()) || userRoles.Contains(Role.SystemAdmin.ToString()))
            {
                return lm.CreatedByUserId == user.Id;
            }

            return false;
        }
    }
}