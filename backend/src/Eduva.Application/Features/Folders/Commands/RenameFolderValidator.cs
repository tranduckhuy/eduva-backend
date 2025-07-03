using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class RenameFolderValidator : AbstractValidator<RenameFolderCommand>
    {
        public RenameFolderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Folder ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Folder name is required")
                .MaximumLength(100).WithMessage("Folder name cannot exceed 100 characters");
        }
    }
}
