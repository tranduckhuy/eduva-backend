using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Commands
{
    public class CreateLessonMaterialValidator : AbstractValidator<CreateLessonMaterialCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateLessonMaterialValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.SchoolId)
                .MustAsync(SchoolExists).WithMessage("The specified school does not exist.")
                .When(x => x.SchoolId.HasValue, ApplyConditionTo.CurrentValidator);

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.ContentType)
                .IsInEnum().WithMessage("Invalid content type specified.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("File size must be greater than zero.");
            RuleFor(x => x.SourceUrl)
                .NotEmpty().WithMessage("Source URL is required.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Source URL must be a valid absolute URL.");
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
            RuleFor(x => x.Tag)
                .MaximumLength(100).WithMessage("Tag must not exceed 100 characters.");
        }

        // Validate if the school exists
        private async Task<bool> SchoolExists(int? schoolId, CancellationToken token)
        {
            if (!schoolId.HasValue) return true;
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            return await schoolRepository.ExistsAsync(schoolId.Value);
        }
    }
}
