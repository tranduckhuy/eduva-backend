using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class UpdateLessonMaterialHandler : IRequestHandler<UpdateLessonMaterialCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLessonMaterialHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateLessonMaterialCommand request, CancellationToken cancellationToken)
        {
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();

            var lessonMaterial = await lessonMaterialRepository.GetByIdAsync(request.Id) ?? throw new LessonMaterialNotFountException(request.Id);

            if (lessonMaterial.CreatedByUserId != request.CreatorId)
            {
                throw new ForbiddenException(["You are not authorized to update this lesson material."]);
            }

            // Update lesson material properties
            lessonMaterial.Title = request.Title ?? lessonMaterial.Title;
            lessonMaterial.Description = request.Description ?? lessonMaterial.Description;
            lessonMaterial.Duration = request.Duration ?? lessonMaterial.Duration;
            lessonMaterial.Visibility = request.Visibility ?? lessonMaterial.Visibility;

            lessonMaterialRepository.Update(lessonMaterial);

            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}
