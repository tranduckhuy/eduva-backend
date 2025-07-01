using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class RestoreFolderValidator : AbstractValidator<RestoreFolderCommand>
    {
        public RestoreFolderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Folder Id is required");
                
            RuleFor(x => x.CurrentUserId)
                .NotEmpty().WithMessage("Current user Id is required");
        }
    }
}
