using Eduva.Application.Common.Validations;
using FluentValidation;

namespace Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks
{
    public class CreateAICreditPackCommandValidator : AbstractValidator<CreateAICreditPackCommand>
    {
        public CreateAICreditPackCommandValidator()
        {
            RuleFor(x => x.Name).ValidateName();
            RuleFor(x => x.Price).ValidatePrice();
            RuleFor(x => x.Credits).ValidateCredits();
            RuleFor(x => x.BonusCredits).ValidateBonusCredits();
        }
    }
}