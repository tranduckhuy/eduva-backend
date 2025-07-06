using FluentValidation;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class CreateSchoolSubscriptionCommandValidator : AbstractValidator<CreateSchoolSubscriptionCommand>
    {
        public CreateSchoolSubscriptionCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .GreaterThan(0)
                .WithMessage("PlanId must be greater than 0.");

            RuleFor(x => x.BillingCycle)
                .IsInEnum()
                .WithMessage("BillingCycle is invalid.");
        }
    }
}