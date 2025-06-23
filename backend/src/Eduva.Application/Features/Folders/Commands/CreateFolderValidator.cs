using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
    {
        public CreateFolderValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
        }
    }
}
