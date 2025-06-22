using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.CreditTransaction;
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

            switch (transaction.PaymentPurpose)
            {
                case PaymentPurpose.SchoolSubscription:
                    await HandleSchoolSubscriptionAsync(transaction, cancellationToken);
                    break;

                case PaymentPurpose.CreditPackage:
                    await HandleCreditPackageAsync(transaction, cancellationToken);
                    break;

                default:
                    throw new InvalidPaymentPurposeException();
            }

            transaction.PaymentStatus = PaymentStatus.Paid;
            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }

        private async Task HandleSchoolSubscriptionAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
        {
            var schoolRepo = _unitOfWork.GetCustomRepository<ISchoolRepository>();
            var school = await schoolRepo.GetByUserIdAsync(transaction.UserId)
                ?? throw new SchoolNotFoundException();

            var planRepo = _unitOfWork.GetCustomRepository<ISubscriptionPlanRepository>();
            var plan = await planRepo.GetByIdAsync(transaction.PaymentItemId)
                ?? throw new PlanNotFoundException();

            var cycle = transaction.Amount == plan.PriceMonthly
                ? BillingCycle.Monthly
                : BillingCycle.Yearly;

            var duration = cycle == BillingCycle.Monthly ? 30 : 365;

            var subRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var oldSub = await subRepo.GetLatestPaidBySchoolIdAsync(school.Id);
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
            await _unitOfWork.CommitAsync();

            transaction.RelatedId = newSub.Id.ToString();

            if (school.Status == EntityStatus.Inactive)
                school.Status = EntityStatus.Active;
        }

        private async Task HandleCreditPackageAsync(PaymentTransaction transaction, CancellationToken cancellationToken)
        {
            var creditPackRepo = _unitOfWork.GetRepository<AICreditPack, int>();
            var pack = await creditPackRepo.GetByIdAsync(transaction.PaymentItemId)
                ?? throw new AICreditPackNotFoundException();

            var totalCredits = pack.Credits + pack.BonusCredits;

            var now = DateTimeOffset.UtcNow;
            var userCreditRepo = _unitOfWork.GetRepository<UserCreditTransaction, Guid>();
            var creditTransaction = new UserCreditTransaction
            {
                UserId = transaction.UserId,
                AICreditPackId = pack.Id,
                PaymentTransactionId = transaction.Id,
                Credits = totalCredits,
                CreatedAt = now
            };

            await userCreditRepo.AddAsync(creditTransaction);
            await _unitOfWork.CommitAsync();


            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(transaction.UserId)
                ?? throw new UserNotExistsException();

            user.TotalCredits += totalCredits;

            transaction.RelatedId = creditTransaction.Id.ToString();

            await _unitOfWork.CommitAsync();
        }

    }
}