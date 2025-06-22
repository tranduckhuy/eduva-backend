using FluentValidation;

namespace Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks
{
    public class UpdateAICreditPackCommandValidator : AbstractValidator<UpdateAICreditPackCommand>
    {
        public UpdateAICreditPackCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Price)
                .NotNull().WithMessage("Price is required.")
                .GreaterThan(0).WithMessage("Price must be greater than 0.");

            RuleFor(x => x.Credits)
                .NotNull().WithMessage("Credits is required.")
                .GreaterThan(0).WithMessage("Credits must be greater than 0.");

            RuleFor(x => x.BonusCredits)
                .NotNull().WithMessage("Bonus credits is required.")
                .GreaterThanOrEqualTo(0).WithMessage("Bonus credits must be 0 or more.");
        }
    }
}