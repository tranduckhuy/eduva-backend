using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial
{
    public class DeleteLessonMaterialHandler : IRequestHandler<DeleteLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storageService;
        private readonly ILogger<DeleteLessonMaterialHandler> _logger;
        private readonly INotificationService _notificationService;

        public DeleteLessonMaterialHandler(IUnitOfWork unitOfWork, ILogger<DeleteLessonMaterialHandler> logger, IStorageService storageService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _storageService = storageService;
            _notificationService = notificationService;
        }

        public async Task<Unit> Handle(DeleteLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();

            var deletedLessonMaterialsBlobNames = new List<string>();

            // Delete all materials marked for deletion if no specific IDs are provided
            if (request.Ids.Count == 0 && request.Permanent)
            {
                var deletedMaterials = await lessonMaterialRepository
                    .FindAsync(x => x.SchoolId == request.SchoolId && x.CreatedByUserId == request.UserId && x.Status == EntityStatus.Deleted, cancellationToken);

                foreach (var material in deletedMaterials)
                {
                    await _notificationService.DeleteNotificationsByLessonMaterialIdAsync(material.Id, cancellationToken);
                    lessonMaterialRepository.Remove(material);
                    deletedLessonMaterialsBlobNames.Add(material.SourceUrl);
                }
                _logger.LogInformation("User {UserId} deleted all lesson materials in school {SchoolId} permanently.", request.UserId, request.SchoolId);
            }
            else
            {
                var materials = await lessonMaterialRepository
                .FindAsync(x => request.Ids.Contains(x.Id), cancellationToken);

                var notFoundIds = request.Ids.Except(materials.Select(m => m.Id)).ToList();
                if (notFoundIds.Count != 0)
                {
                    throw new LessonMaterialNotFoundException(notFoundIds);
                }

                foreach (var material in materials)
                {
                    if (material.SchoolId != request.SchoolId)
                    {
                        throw new ForbiddenException(["You do not have permission to delete because the material does not belong to your school."]);
                    }

                    if (material.CreatedByUserId != request.UserId)
                    {
                        throw new ForbiddenException(["You can only delete materials that you created."]);
                    }

                    if (request.Permanent)
                    {
                        await _notificationService.DeleteNotificationsByLessonMaterialIdAsync(material.Id, cancellationToken);

                        lessonMaterialRepository.Remove(material);
                        deletedLessonMaterialsBlobNames.Add(material.SourceUrl);
                    }
                    else
                    {
                        material.Status = EntityStatus.Deleted;
                        lessonMaterialRepository.Update(material);
                    }

                    _logger.LogInformation("User {UserId} deleted lesson material {MaterialId} in school {SchoolId}. Permanent: {Permanent}",
                        request.UserId, material.Id, request.SchoolId, request.Permanent);
                }
            }

            if (deletedLessonMaterialsBlobNames.Count > 0)
            {
                await _storageService.DeleteRangeFileAsync(deletedLessonMaterialsBlobNames, true);
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}
