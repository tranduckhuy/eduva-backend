using FluentValidation;

namespace Eduva.Application.Features.Folders.Commands
{
    public class UpdateFolderOrderValidator : AbstractValidator<UpdateFolderOrderCommand>
    {
        public UpdateFolderOrderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Folder ID is required");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be a non-negative number");
        }
    }
}
