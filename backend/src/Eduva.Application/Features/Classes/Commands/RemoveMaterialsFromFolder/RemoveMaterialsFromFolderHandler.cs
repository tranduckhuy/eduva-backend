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
                throw new AppException(CustomCode.FolderNotFound);

            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);

                if (classroom == null)
                    throw new AppException(CustomCode.ClassNotFound);

                if (classroom.Status != EntityStatus.Active)
                    throw new AppException(CustomCode.ClassAlreadyArchived);
            }

            // Check access permission
            await CheckFolderAccessAsync(folder, request.CurrentUserId);

            var folderLessonMaterialRepo = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
            var lessonMaterialRepo = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lessonMaterialQuestionsRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, int>();
            var lessonMaterialsApproveRepo = _unitOfWork.GetRepository<LessonMaterialApproval, int>();

            var allFolderMaterials = await folderLessonMaterialRepo.GetAllAsync();

            List<FolderLessonMaterial> folderMaterials;
            if (request.MaterialIds != null && request.MaterialIds.Count > 0)
            {
                folderMaterials = allFolderMaterials
                    .Where(flm => flm.FolderId == request.FolderId && request.MaterialIds.Contains(flm.LessonMaterialId))
                    .ToList();
            }
            else
            {
                folderMaterials = allFolderMaterials
                    .Where(flm => flm.FolderId == request.FolderId)
                    .ToList();
            }

            if (folderMaterials.Count == 0)
                throw new AppException(CustomCode.LessonMaterialNotFoundInFolder);

            foreach (var folderMaterial in folderMaterials)
            {
                if (folder.OwnerType == OwnerType.Class)
                {
                    folderLessonMaterialRepo.Remove(folderMaterial);

                    var isOnlyUsedHere = !allFolderMaterials.Any(flm => flm.LessonMaterialId == folderMaterial.LessonMaterialId && flm.FolderId != folder.Id);

                    if (isOnlyUsedHere)
                    {
                        var allQuestions = await lessonMaterialQuestionsRepo.GetAllAsync();
                        var questions = allQuestions.Where(q => q.LessonMaterialId == folderMaterial.LessonMaterialId).ToList();
                        lessonMaterialQuestionsRepo.RemoveRange(questions);

                        var allApproves = await lessonMaterialsApproveRepo.GetAllAsync();
                        var approves = allApproves.Where(a => a.LessonMaterialId == folderMaterial.LessonMaterialId).ToList();
                        lessonMaterialsApproveRepo.RemoveRange(approves);

                        var lessonMaterial = await lessonMaterialRepo.GetByIdAsync(folderMaterial.LessonMaterialId);
                        if (lessonMaterial != null)
                        {
                            lessonMaterialRepo.Remove(lessonMaterial);
                        }
                    }
                }
                else if (folder.OwnerType == OwnerType.Personal)
                {
                    var lessonMaterial = await lessonMaterialRepo.GetByIdAsync(folderMaterial.LessonMaterialId);
                    if (lessonMaterial != null && lessonMaterial.Status != EntityStatus.Deleted)
                    {
                        lessonMaterial.Status = EntityStatus.Deleted;
                        lessonMaterialRepo.Update(lessonMaterial);
                    }
                    folderLessonMaterialRepo.Remove(folderMaterial);
                }
            }

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

            if (folder.OwnerType == OwnerType.Personal && folder.UserId == userId)
            {
                return;
            }

            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                var classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);

                if (classroom == null)
                {
                    throw new AppException(CustomCode.ClassNotFound);
                }

                if ((roles.Contains(nameof(Role.Teacher)) || roles.Contains(nameof(Role.ContentModerator))) && classroom.TeacherId == userId)
                {
                    return;
                }

                if (roles.Contains(nameof(Role.SchoolAdmin)) && classroom.SchoolId == user.SchoolId)
                {
                    return;
                }
            }

            throw new AppException(CustomCode.Unauthorized);
        }
    }
}