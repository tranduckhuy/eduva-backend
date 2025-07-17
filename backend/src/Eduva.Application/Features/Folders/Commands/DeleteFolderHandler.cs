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
    public class DeleteFolderHandler : IRequestHandler<DeleteFolderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteFolderHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
        {
            var folderRepository = _unitOfWork.GetCustomRepository<IFolderRepository>();
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();

            var folder = await folderRepository.GetFolderWithMaterialsAsync(request.Id);

            if (folder == null)
                throw new AppException(CustomCode.FolderNotFound);

            if (!await HasPermissionToUpdateFolder(folder, request.CurrentUserId))
                throw new AppException(CustomCode.Forbidden);

            if (folder.Status == EntityStatus.Deleted)
                return true;

            if (folder.OwnerType == OwnerType.Personal && folder.Status != EntityStatus.Archived)
                throw new AppException(CustomCode.FolderShouldBeArchivedBeforeDelete);

            try
            {
                if (folder.OwnerType == OwnerType.Class)
                {
                    foreach (var link in folder.FolderLessonMaterials.ToList())
                    {
                        folderLessonMaterialRepository.Remove(link);
                    }
                }
                else if (folder.OwnerType == OwnerType.Personal)
                {
                    var lessonMaterialQuestionsRepo = _unitOfWork.GetRepository<LessonMaterialQuestion, int>();
                    var lessonMaterialsApproveRepo = _unitOfWork.GetRepository<LessonMaterialApproval, int>();

                    foreach (var link in folder.FolderLessonMaterials.ToList())
                    {
                        folderLessonMaterialRepository.Remove(link);

                        if (link.LessonMaterial != null)
                        {
                            var lessonMaterialId = link.LessonMaterial.Id;

                            var isOnlyUsedHere = await folderLessonMaterialRepository
                                .CountAsync(flm => flm.LessonMaterialId == lessonMaterialId && flm.FolderId != folder.Id, cancellationToken) == 0;

                            if (isOnlyUsedHere)
                            {
                                var allQuestions = await lessonMaterialQuestionsRepo.GetAllAsync();
                                var questions = allQuestions.Where(q => q.LessonMaterialId == lessonMaterialId).ToList();
                                lessonMaterialQuestionsRepo.RemoveRange(questions);

                                var allApproves = await lessonMaterialsApproveRepo.GetAllAsync();
                                var approves = allApproves.Where(a => a.LessonMaterialId == lessonMaterialId).ToList();
                                lessonMaterialsApproveRepo.RemoveRange(approves);

                                lessonMaterialRepository.Remove(link.LessonMaterial);
                            }
                        }
                    }
                }

                folderRepository.Remove(folder);
                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.FolderDeleteFailed);
            }
        }

        public async Task<bool> HasPermissionToUpdateFolder(Folder folder, Guid userId)
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

            // System Admin can delete any folder
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

                if ((userRoles.Contains(Role.Teacher.ToString()) || userRoles.Contains(Role.ContentModerator.ToString()))
                    && classroom.TeacherId == userId)
                {
                    return true;
                }

                // School Admin can delete any folder from their school
                if (userRoles.Contains(Role.SchoolAdmin.ToString()) && user.SchoolId == classroom.SchoolId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
