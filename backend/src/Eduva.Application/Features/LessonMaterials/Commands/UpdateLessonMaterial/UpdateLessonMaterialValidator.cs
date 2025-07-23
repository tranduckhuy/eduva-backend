using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Commands.UpdateLessonMaterial
{
    public class UpdateLessonMaterialValidator : AbstractValidator<UpdateLessonMaterialCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLessonMaterialValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Lesson material ID is required.")
                .MustAsync(LessonMaterialExists).WithMessage("The specified lesson material does not exist.");

            RuleFor(x => x.Title)
                .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.")
                .When(x => !string.IsNullOrEmpty(x.Title), ApplyConditionTo.CurrentValidator);

            RuleFor(x => x.Duration)
                .GreaterThanOrEqualTo(0).WithMessage("Duration must be a non-negative integer.")
                .When(x => x.Duration.HasValue, ApplyConditionTo.CurrentValidator);

            RuleFor(x => x.Visibility)
                .IsInEnum().WithMessage("Visibility must be a valid enum value.")
                .When(x => x.Visibility.HasValue, ApplyConditionTo.CurrentValidator);
        }

        private async Task<bool> LessonMaterialExists(Guid id, CancellationToken token)
        {
            var lessonMaterialRepository = _unitOfWork.GetRepository<LessonMaterial, Guid>();
            return await lessonMaterialRepository.ExistsAsync(id);
        }
    }
}
