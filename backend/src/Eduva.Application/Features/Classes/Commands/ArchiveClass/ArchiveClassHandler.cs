using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands.ArchiveClass
{
    public class ArchiveClassHandler : IRequestHandler<ArchiveClassCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        public ArchiveClassHandler(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Unit> Handle(ArchiveClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Check if the class is already archived
            if (classroom.Status == EntityStatus.Archived)
            {
                throw new AppException(CustomCode.ClassAlreadyArchived);
            }

            // Get the current user
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var currentUser = await userRepository.GetByIdAsync(request.TeacherId)
                ?? throw new AppException(CustomCode.UserNotExists);

            // Check user roles
            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool isTeacherOfClass = classroom.TeacherId == request.TeacherId;
            bool isAdmin = userRoles.Contains(nameof(Role.SystemAdmin)) || userRoles.Contains(nameof(Role.SchoolAdmin));

            if (!isTeacherOfClass && !isAdmin)
                throw new AppException(CustomCode.NotTeacherOfClass);

            try
            {
                // Archive class
                classroom.Status = EntityStatus.Archived;
                classroom.LastModifiedAt = DateTimeOffset.UtcNow;
                classroomRepository.Update(classroom);

                // Archive all folders in the class
                var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
                var folders = await folderRepository.FindAsync(f => f.ClassId == classroom.Id);

                foreach (var folder in folders)
                {
                    folder.Status = EntityStatus.Archived;
                    folder.LastModifiedAt = DateTimeOffset.UtcNow;
                    folderRepository.Update(folder);
                }

                // Get all lesson materials in class folders
                var folderLessonMaterialRepository = _unitOfWork.GetRepository<FolderLessonMaterial, int>();
                var folderIds = folders.Select(f => f.Id).ToList();

                var folderLessonMaterials = await folderLessonMaterialRepository
                    .FindAsync(flm => folderIds.Contains(flm.FolderId));

                var lessonMaterialIds = folderLessonMaterials.Select(flm => flm.LessonMaterialId).Distinct().ToList();

                // Remove all questions from lesson materials in the class
                var lessonMaterialQuestionRepository = _unitOfWork.GetRepository<LessonMaterialQuestion, Guid>();
                var questionsToRemove = await lessonMaterialQuestionRepository
                    .FindAsync(lmq => lessonMaterialIds.Contains(lmq.LessonMaterialId));

                foreach (var question in questionsToRemove)
                {
                    lessonMaterialQuestionRepository.Remove(question);
                }

                // Remove all lesson materials from class folders
                foreach (var folderLessonMaterial in folderLessonMaterials)
                {
                    folderLessonMaterialRepository.Remove(folderLessonMaterial);
                }

                await _unitOfWork.CommitAsync();
                return Unit.Value;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.ClassArchiveFailed);
            }
        }
    }
}
