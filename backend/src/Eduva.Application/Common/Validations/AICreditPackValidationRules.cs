using FluentValidation;

namespace Eduva.Application.Common.Validations
{
    public static class AICreditPackValidationRules
    {
        public static IRuleBuilderOptions<T, string> ValidateName<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        }

        public static IRuleBuilderOptions<T, decimal> ValidatePrice<T>(this IRuleBuilder<T, decimal> ruleBuilder)
        {
            return ruleBuilder
                .NotNull().WithMessage("Price is required.")
                .GreaterThan(0).WithMessage("Price must be greater than 0.");
        }

        public static IRuleBuilderOptions<T, int> ValidateCredits<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder
                .NotNull().WithMessage("Credits is required.")
                .GreaterThan(0).WithMessage("Credits must be greater than 0.");
        }

        public static IRuleBuilderOptions<T, int> ValidateBonusCredits<T>(this IRuleBuilder<T, int> ruleBuilder)
        {
            return ruleBuilder
                .NotNull().WithMessage("Bonus credits is required.")
                .GreaterThanOrEqualTo(0).WithMessage("Bonus credits must be 0 or more.");
        }
    }
}