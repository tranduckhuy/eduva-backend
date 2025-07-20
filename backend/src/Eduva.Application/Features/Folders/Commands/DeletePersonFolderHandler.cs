using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Folders.Commands
{
    public class DeletePersonFolderHandler : IRequestHandler<DeletePersonFolderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFolderRepository _folderRepository;
        private readonly ILessonMaterialRepository _lessonMaterialRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeletePersonFolderHandler(
            IUnitOfWork unitOfWork,
            IFolderRepository folderRepository,
            ILessonMaterialRepository lessonMaterialRepository,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _folderRepository = folderRepository;
            _lessonMaterialRepository = lessonMaterialRepository;
            _userManager = userManager;
        }

        private async Task<bool> HasPermissionToDeletePersonFolder(Guid currentUserId, Folder folder)
        {
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(currentUserId);
            if (user == null)
                return false;

            var isSystemAdmin = await _userManager.IsInRoleAsync(user, Role.SystemAdmin.ToString());
            if (isSystemAdmin)
                return true;

            return folder.UserId == currentUserId;
        }

        public async Task<bool> Handle(DeletePersonFolderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.FolderIds.Count > 0)
                {
                    var checkFolders = await _folderRepository.ListAsync(
                        f => request.FolderIds.Contains(f.Id),
                        cancellationToken);

                    if (checkFolders.Any(f => f.OwnerType != OwnerType.Personal))
                        throw new AppException(CustomCode.FolderMustBePersonal);
                }

                var foldersToDelete = request.FolderIds.Count == 0
                    ? await _folderRepository.ListAsync(
                        f => f.UserId == request.CurrentUserId &&
                             f.OwnerType == OwnerType.Personal &&
                             f.Status == EntityStatus.Archived,
                        cancellationToken)
                    : await _folderRepository.ListAsync(
                        f => request.FolderIds.Contains(f.Id) &&
                             f.OwnerType == OwnerType.Personal,
                        cancellationToken);

                foreach (var folder in foldersToDelete)
                {
                    if (!await HasPermissionToDeletePersonFolder(request.CurrentUserId, folder))
                        throw new AppException(CustomCode.Forbidden);

                    if (folder.Status == EntityStatus.Deleted)
                        continue;

                    if (folder.OwnerType == OwnerType.Personal && folder.Status != EntityStatus.Archived)
                        throw new AppException(CustomCode.FolderShouldBeArchivedBeforeDelete);

                    var folderWithMaterials = await _folderRepository.GetFolderWithMaterialsAsync(folder.Id);
                    var folderLessonMaterials = folderWithMaterials?.FolderLessonMaterials?.ToList() ?? new List<FolderLessonMaterial>();

                    foreach (var link in folderLessonMaterials.Where(l => l.LessonMaterial != null))
                    {
                        var lessonMaterial = link.LessonMaterial!;
                        if (lessonMaterial.CreatedByUserId == folder.UserId && lessonMaterial.Status != EntityStatus.Deleted)
                        {
                            lessonMaterial.Status = EntityStatus.Deleted;
                            _lessonMaterialRepository.Update(lessonMaterial);
                        }
                    }

                    var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
                    folderLessonMaterialRepository.RemoveRange(folderLessonMaterials);

                    _folderRepository.Remove(folder);
                }

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.FolderDeleteFailed);
            }
        }
    }
}