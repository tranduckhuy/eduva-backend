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

            RuleFor(x => x)
                .MustAsync(UserBelongsToSchool).WithMessage("User does not belong to the specified school.")
                .When(x => x.SchoolId.HasValue, ApplyConditionTo.CurrentValidator);

            RuleFor(x => x.FolderId)
                .NotNull().WithMessage("Folder ID is required.")
                .MustAsync(FolderExists).WithMessage("The specified folder does not exist.");

            RuleFor(x => x.BlobNames)
                .NotNull().WithMessage("Blob names list is required.")
                .NotEmpty().WithMessage("At least one blob name must be provided.")
                .Must(x => x == null || x.Count <= 10).WithMessage("Cannot upload more than 10 lesson materials at once.");
            RuleFor(x => x.LessonMaterials)
                .NotNull().WithMessage("Lesson materials list is required.")
                .NotEmpty().WithMessage("At least one lesson material must be provided.")
                .Must(x => x == null || x.Count <= 10).WithMessage("Cannot upload more than 10 lesson materials at once.");

            RuleForEach(x => x.LessonMaterials)
                .SetValidator(new LessonMaterialRequestValidator());
        }

        private async Task<bool> SchoolExists(int? schoolId, CancellationToken token)
        {
            if (!schoolId.HasValue) return false;
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            return await schoolRepository.ExistsAsync(schoolId.Value);
        }

        // Validate if the folder exists
        private async Task<bool> FolderExists(Guid folderId, CancellationToken token)
        {
            if (folderId == Guid.Empty) return false;
            var folderRepository = _unitOfWork.GetRepository<Folder, Guid>();
            return await folderRepository.ExistsAsync(folderId);
        }

        // Validate if the user belongs to the specified school
        private async Task<bool> UserBelongsToSchool(CreateLessonMaterialCommand command, CancellationToken token)
        {
            if (!command.SchoolId.HasValue) return true; // If no school specified, skip this validation

            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepository.GetByIdAsync(command.CreatedBy);

            return user != null && user.SchoolId == command.SchoolId.Value;
        }
    }
    public class LessonMaterialRequestValidator : AbstractValidator<LessonMaterialRequest>
    {
        public LessonMaterialRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(255).WithMessage("Title must not exceed 255 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.ContentType)
                .IsInEnum().WithMessage("Invalid content type specified.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("File size must be greater than zero.")
                .LessThanOrEqualTo(1073741824).WithMessage("File size must not exceed 1GB (1073741824 bytes).");

            RuleFor(x => x.SourceUrl)
                .NotEmpty().WithMessage("Source URL is required.");
        }
    }
}
