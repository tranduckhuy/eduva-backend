using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan
{
    public class CreateSubscriptionPlanCommandValidator : AbstractValidator<CreateSubscriptionPlanCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateSubscriptionPlanCommandValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Plan name is required.")
                .MaximumLength(255)
                .MustAsync(NameIsUnique).WithMessage("Plan name already exists.");

            RuleFor(x => x.PriceMonthly)
                .GreaterThanOrEqualTo(0).WithMessage("Monthly price must be >= 0.");

            RuleFor(x => x.PricePerYear)
                .GreaterThanOrEqualTo(0).WithMessage("Yearly price must be >= 0.");

            RuleFor(x => x.MaxUsers)
                .GreaterThan(0).WithMessage("Max users must be greater than 0.");

            RuleFor(x => x.StorageLimitGB)
                .GreaterThanOrEqualTo(0).WithMessage("Storage limit must be >= 0.");
        }

        private async Task<bool> NameIsUnique(string name, CancellationToken token)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            return !await repo.ExistsAsync(p => p.Name == name);
        }
    }
}