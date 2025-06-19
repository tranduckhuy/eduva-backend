using Eduva.Application.Features.Classes.Commands;
using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands
{
    public class UpdateClassValidator : AbstractValidator<UpdateClassCommand>
    {
        public UpdateClassValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Class name is required.")
                .MaximumLength(100).WithMessage("Class name must not exceed 100 characters.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid status value.");
        }
    }
}
