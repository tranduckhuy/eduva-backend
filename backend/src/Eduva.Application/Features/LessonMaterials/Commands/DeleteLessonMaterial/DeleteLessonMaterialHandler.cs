using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial
{
    public class DeleteLessonMaterialHandler : IRequestHandler<DeleteLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteLessonMaterialHandler> _logger;

        public DeleteLessonMaterialHandler(IUnitOfWork unitOfWork, ILogger<DeleteLessonMaterialHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        public async Task<Unit> Handle(DeleteLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();

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

                if (request.Permanent && material.Status == EntityStatus.Deleted)
                {
                    lessonMaterialRepository.Remove(material);
                }
                else
                {
                    material.Status = EntityStatus.Deleted;
                    lessonMaterialRepository.Update(material);
                }

                _logger.LogInformation("User {UserId} deleted lesson material {MaterialId} in school {SchoolId}. Permanent: {Permanent}",
                    request.UserId, material.Id, request.SchoolId, request.Permanent);
            }

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}
