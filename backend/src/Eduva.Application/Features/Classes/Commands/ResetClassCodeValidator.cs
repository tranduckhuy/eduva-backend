using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands
{
    public class ResetClassCodeValidator : AbstractValidator<ResetClassCodeCommand>
    {
        public ResetClassCodeValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required.");
        }
    }
}
