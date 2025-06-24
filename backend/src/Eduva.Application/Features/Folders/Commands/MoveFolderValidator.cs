using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class MoveFolderValidator : AbstractValidator<MoveFolderCommand>
    {
        public MoveFolderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Folder ID is required");

            RuleFor(x => x.OwnerType)
                .IsInEnum().WithMessage("Invalid owner type");
                
            RuleFor(x => x.ClassId)
                .NotEmpty().When(x => x.OwnerType == Domain.Enums.OwnerType.Class)
                .WithMessage("Class ID is required when owner type is Class");
        }
    }
}
