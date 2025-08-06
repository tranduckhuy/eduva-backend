using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands.AddMaterialsToFolder
{
    public class AddMaterialsToFolderHandler : IRequestHandler<AddMaterialsToFolderCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddMaterialsToFolderHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<bool> Handle(AddMaterialsToFolderCommand request, CancellationToken cancellationToken)
        {
            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
            var folder = await folderRepository.GetByIdAsync(request.FolderId);
            if (folder == null)
            {
                throw new AppException(CustomCode.FolderNotFound);
            }

            Classroom? classroom = null;
            if (folder.OwnerType == OwnerType.Class && folder.ClassId.HasValue)
            {
                var classRepository = _unitOfWork.GetRepository<Classroom, Guid>();
                classroom = await classRepository.GetByIdAsync(folder.ClassId.Value);
                if (classroom == null)
                {
                    throw new AppException(CustomCode.ClassNotFound);
                }
                if (classroom.Status != EntityStatus.Active)
                {
                    throw new AppException(CustomCode.ClassAlreadyArchived);
                }

                if (request.ClassId != Guid.Empty && folder.ClassId != request.ClassId)
                {
                    throw new AppException(CustomCode.Unauthorized);
                }
            }

            // Check access permission
            await CheckFolderAccessAsync(folder, request.CurrentUserId);

            // Validate materials exist and belong to the user
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();

            foreach (var materialId in request.MaterialIds)
            {
                var material = await lessonMaterialRepository.GetByIdAsync(materialId);
                if (material == null)
                    throw new AppException(CustomCode.LessonMaterialNotFound);

                if (material.LessonStatus != LessonMaterialStatus.Approved || material.Status != EntityStatus.Active)
                    throw new AppException(CustomCode.LessonMaterialNotApproved);

                bool canAddSharedMaterial = false;
                if (classroom != null && material.Visibility == LessonMaterialVisibility.School && material.SchoolId == classroom.SchoolId)
                {
                    canAddSharedMaterial = true;
                }

                if (material.CreatedByUserId != request.CurrentUserId && !canAddSharedMaterial)
                    throw new AppException(CustomCode.Unauthorized);

                if (classroom != null)
                {
                    bool existsInAnyFolderOfClass = await folderLessonMaterialRepository.ExistsAsync(flm =>
                        flm.LessonMaterialId == materialId &&
                        flm.Folder.ClassId == classroom.Id);

                    if (existsInAnyFolderOfClass)
                        throw new AppException(CustomCode.LessonMaterialAlreadyExistsInClassFolder);
                }

                // Check if material is already in this folder
                bool alreadyInFolder = await folderLessonMaterialRepository.ExistsAsync(flm =>
                    flm.FolderId == request.FolderId && flm.LessonMaterialId == materialId);

                if (!alreadyInFolder)
                {
                    // Add material to folder
                    var folderLessonMaterial = new FolderLessonMaterial
                    {
                        FolderId = request.FolderId,
                        LessonMaterialId = materialId
                    };

                    await folderLessonMaterialRepository.AddAsync(folderLessonMaterial);
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

            // Check if folder belongs to user
            if (folder.OwnerType == OwnerType.Personal && folder.UserId == userId)
            {
                return;
            }

            // Check if folder belongs to a class
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

                // School admin of the same school
                if (roles.Contains(nameof(Role.SchoolAdmin)) && classroom.SchoolId == user.SchoolId)
                {
                    return;
                }

                // Student enrolled in the class
                if (roles.Contains(nameof(Role.Student)))
                {
                    var studentClassRepository = _unitOfWork.GetRepository<StudentClass, Guid>();
                    bool isStudentInClass = await studentClassRepository.ExistsAsync(sc =>
                        sc.ClassId == classroom.Id && sc.StudentId == userId);

                    if (isStudentInClass)
                    {
                        return;
                    }
                }
            }

            throw new AppException(CustomCode.Unauthorized);
        }
    }
}