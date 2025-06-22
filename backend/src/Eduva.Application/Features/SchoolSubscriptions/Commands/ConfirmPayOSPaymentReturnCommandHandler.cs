using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class ConfirmPayOSPaymentReturnCommandHandler : IRequestHandler<ConfirmPayOSPaymentReturnCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConfirmPayOSPaymentReturnCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ConfirmPayOSPaymentReturnCommand request, CancellationToken cancellationToken)
        {
            if (request.Code != "00" || request.Status != "PAID")
                throw new PaymentFailedException();

            var transactionRepo = _unitOfWork.GetCustomRepository<IPaymentTransactionRepository>();
            var transaction = await transactionRepo.GetByTransactionCodeAsync(request.OrderCode.ToString(), cancellationToken)
                ?? throw new PaymentTransactionNotFoundException();

            if (transaction.PaymentStatus == PaymentStatus.Paid)
                throw new PaymentAlreadyConfirmedException();

            var schoolRepo = _unitOfWork.GetCustomRepository<ISchoolRepository>();
            var school = await schoolRepo.GetByUserIdAsync(transaction.UserId)
                ?? throw new SchoolNotFoundException();

            var planRepo = _unitOfWork.GetCustomRepository<ISubscriptionPlanRepository>();
            var plan = await planRepo.GetPlanByTransactionIdAsync(transaction.Id)
                ?? throw new PlanNotFoundException();

            var amount = transaction.Amount;
            var cycle = (amount == plan.PriceMonthly) ? BillingCycle.Monthly : BillingCycle.Yearly;
            var duration = cycle == BillingCycle.Monthly ? 30 : 365;

            var subRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var oldSub = await subRepo.GetLatestPaidBySchoolIdAsync(school.Id, cancellationToken);
            if (oldSub is not null && oldSub.SubscriptionStatus == SubscriptionStatus.Active)
            {
                oldSub.SubscriptionStatus = SubscriptionStatus.Expired;
                oldSub.EndDate = DateTimeOffset.UtcNow;
            }

            var now = DateTimeOffset.UtcNow;
            var newSub = new SchoolSubscription
            {
                StartDate = now,
                EndDate = now.AddDays(duration),
                SubscriptionStatus = SubscriptionStatus.Active,
                BillingCycle = cycle,
                SchoolId = school.Id,
                PlanId = plan.Id,
                PaymentTransactionId = transaction.Id,
                CreatedAt = now
            };

            await subRepo.AddAsync(newSub);

            transaction.PaymentStatus = PaymentStatus.Paid;
            transaction.RelatedId = newSub.Id.ToString();

            if (school.Status == EntityStatus.Inactive)
                school.Status = EntityStatus.Active;

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}