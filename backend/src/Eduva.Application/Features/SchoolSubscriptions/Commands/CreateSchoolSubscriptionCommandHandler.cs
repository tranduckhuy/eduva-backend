using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SchoolSubscriptions.Response;
using Eduva.Application.Features.SubscriptionPlans.Configurations;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class CreateSchoolSubscriptionCommandHandler : IRequestHandler<CreateSchoolSubscriptionCommand, (CustomCode, CreatePaymentLinkResponse)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayOS _payOS;
        private readonly PayOSConfig _payOSConfig;

        public CreateSchoolSubscriptionCommandHandler(IUnitOfWork unitOfWork, PayOS payOS, IOptions<PayOSConfig> payOSOptions)
        {
            _unitOfWork = unitOfWork;
            _payOS = payOS;
            _payOSConfig = payOSOptions.Value;
        }

        public async Task<(CustomCode, CreatePaymentLinkResponse)> Handle(CreateSchoolSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var schoolRepo = _unitOfWork.GetRepository<School, int>();
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var schoolSubRepo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();

            var school = await schoolRepo.GetByIdAsync(request.SchoolId)
                ?? throw new SchoolNotFoundException();

            var plan = await planRepo.GetByIdAsync(request.PlanId)
                ?? throw new PlanNotFoundException();

            if (plan.Status != EntityStatus.Active)
                throw new PlanNotActiveException();

            var amount = request.BillingCycle == BillingCycle.Monthly
                ? plan.PriceMonthly
                : plan.PricePerYear;

            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var transactionId = orderCode;
            var now = DateTimeOffset.UtcNow;

            decimal? deductedAmount = null;
            double? deductedPercent = null;

            var existingSub = await schoolSubRepo.GetActiveSubscriptionBySchoolIdAsync(school.Id);

            if (existingSub != null)
            {
                if (existingSub.PlanId == request.PlanId && existingSub.BillingCycle == request.BillingCycle)
                {
                    throw new SchoolSubscriptionAlreadyExistsException();
                }

                var isDowngrade = (existingSub.BillingCycle == BillingCycle.Yearly && request.BillingCycle == BillingCycle.Monthly)
                     || (request.BillingCycle == BillingCycle.Monthly && plan.PriceMonthly < existingSub.Plan.PriceMonthly)
                     || (request.BillingCycle == BillingCycle.Yearly && plan.PricePerYear < existingSub.Plan.PricePerYear);

                if (isDowngrade)
                {
                    throw new DowngradeNotAllowedException();
                }

                var totalDaysOld = existingSub.BillingCycle == BillingCycle.Monthly ? 30 : 365;
                var daysUsed = (now - existingSub.StartDate).TotalDays;
                var daysLeft = Math.Max(0, totalDaysOld - daysUsed);
                var oldDailyRate = (double)existingSub.AmountPaid / totalDaysOld;
                deductedAmount = (decimal)(daysLeft * oldDailyRate);
                deductedPercent = (double)(deductedAmount ?? 0) / (double)(request.BillingCycle == BillingCycle.Monthly ? plan.PriceMonthly : plan.PricePerYear) * 100;
                amount -= deductedAmount ?? 0;

                if (amount <= 10000)
                {
                    amount = 10000;
                }
            }

            var subscription = new SchoolSubscription
            {
                PlanId = plan.Id,
                SchoolId = school.Id,
                BillingCycle = request.BillingCycle,
                SubscriptionStatus = SubscriptionStatus.Peding,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.PayOS,
                TransactionId = transactionId.ToString(),
                AmountPaid = amount,
                StartDate = now,
                EndDate = request.BillingCycle == BillingCycle.Monthly ? now.AddMonths(1) : now.AddYears(1),
                LastUsageResetDate = now,
                PurchasedAt = now,
                CurrentPeriodAIUsageMinutes = 0
            };

            await schoolSubRepo.AddAsync(subscription);
            await _unitOfWork.CommitAsync();

            var billingCode = request.BillingCycle == BillingCycle.Monthly ? "M" : "Y";

            var paymentRequest = new PaymentData(
                orderCode: orderCode,
                amount: (int)amount,
               description: $"{plan.Name}{billingCode}",
                items: new List<ItemData>
                {
                    new ItemData(
                        name: plan.Name,
                        quantity: 1,
                        price: (int)amount
                    )
                },
                cancelUrl: _payOSConfig.CancelUrl,
                returnUrl: _payOSConfig.ReturnUrl,
                buyerName: school.Name,
                buyerEmail: school.ContactEmail,
                buyerPhone: school.ContactPhone
            );

            var result = await _payOS.createPaymentLink(paymentRequest);

            var response = new CreatePaymentLinkResponse
            {
                CheckoutUrl = result.checkoutUrl,
                PaymentLinkId = result.paymentLinkId,
                Amount = (long)amount,
                DeductedAmount = deductedAmount.HasValue ? Math.Round(deductedAmount.Value, 2) : 0,
                DeductedPercent = deductedPercent.HasValue ? Math.Round(deductedPercent.Value, 2) : 0,
                TransactionId = transactionId.ToString(),
            };

            return (CustomCode.Success, response);
        }
    }
}