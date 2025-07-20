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
        private ILogger<DeleteLessonMaterialHandler> _logger;

        public DeleteLessonMaterialHandler(IUnitOfWork unitOfWork, ILogger<DeleteLessonMaterialHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeleteLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            var lessonMaterial = await lessonMaterialRepository.GetByIdAsync(request.Id) ?? throw new LessonMaterialNotFountException(request.Id);

            if (lessonMaterial.SchoolId != request.SchoolId)
            {
                throw new UnauthorizedException(["Unauthorized to delete this lesson material. The specified school ID does not match the lesson material's school ID."]);
            }

            if (request.Permanent && lessonMaterial.Status == EntityStatus.Deleted)
            {
                // Permanently delete the lesson material
                lessonMaterialRepository.Remove(lessonMaterial);
            }
            else
            {
                lessonMaterial.Status = EntityStatus.Deleted;
                lessonMaterialRepository.Update(lessonMaterial);
            }

            _logger.LogInformation("User {UserId} deleted lesson material {LessonMaterialId} in school {SchoolId}. Permanent: {Permanent}",
                request.UserId, request.Id, request.SchoolId, request.Permanent);

            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}
