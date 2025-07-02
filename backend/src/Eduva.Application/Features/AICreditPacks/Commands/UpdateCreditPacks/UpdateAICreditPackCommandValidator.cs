using Eduva.Application.Common.Validations;
using FluentValidation;

namespace Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks
{
    public class UpdateAICreditPackCommandValidator : AbstractValidator<UpdateAICreditPackCommand>
    {
        public UpdateAICreditPackCommandValidator()
        {
            RuleFor(x => x.Name).ValidateName();
            RuleFor(x => x.Price).ValidatePrice();
            RuleFor(x => x.Credits).ValidateCredits();
            RuleFor(x => x.BonusCredits).ValidateBonusCredits();
        }
    }
}