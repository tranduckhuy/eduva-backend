using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class ArchiveFolderValidator : AbstractValidator<ArchiveFolderCommand>
    {
        public ArchiveFolderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Folder Id is required");
                
            RuleFor(x => x.CurrentUserId)
                .NotEmpty().WithMessage("Current user Id is required");
        }
    }
}
