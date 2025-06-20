using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SchoolSubscriptions.Configurations;
using Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOSService;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.Extensions.Options;
using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class CreateSchoolSubscriptionCommandHandler : IRequestHandler<CreateSchoolSubscriptionCommand, (CustomCode, CreatePaymentLinkResponse)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayOSService _payOSService;
        private readonly PayOSConfig _payOSConfig;

        public CreateSchoolSubscriptionCommandHandler(IUnitOfWork unitOfWork, IPayOSService payOSService, IOptions<PayOSConfig> payOSOptions)
        {
            _unitOfWork = unitOfWork;
            _payOSService = payOSService;
            _payOSConfig = payOSOptions.Value;
        }

        public async Task<(CustomCode, CreatePaymentLinkResponse)> Handle(CreateSchoolSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var school = await GetSchoolAsync(request.SchoolId);
            var plan = await GetPlanAsync(request.PlanId);
            var amount = GetBaseAmount(plan, request.BillingCycle);
            var now = DateTimeOffset.UtcNow;
            var orderCode = now.ToUnixTimeSeconds();
            var transactionId = orderCode.ToString();

            var (finalAmount, deductedAmount, deductedPercent) = await CalculateFinalAmountAsync(request, school.Id, plan, amount, now);

            var subscription = CreateNewSubscription(request, plan, school.Id, finalAmount, now, transactionId);
            await SaveSubscriptionAsync(subscription);

            var paymentRequest = BuildPaymentRequest(plan, request.BillingCycle, (int)finalAmount, school, orderCode);
            var result = await _payOSService.CreatePaymentLinkAsync(paymentRequest);

            var response = new CreatePaymentLinkResponse
            {
                CheckoutUrl = result.checkoutUrl,
                PaymentLinkId = result.paymentLinkId,
                Amount = (long)finalAmount,
                DeductedAmount = deductedAmount,
                DeductedPercent = deductedPercent,
                TransactionId = transactionId,
            };

            return (CustomCode.Success, response);
        }

        private async Task<School> GetSchoolAsync(int schoolId)
        {
            var repo = _unitOfWork.GetRepository<School, int>();
            return await repo.GetByIdAsync(schoolId) ?? throw new SchoolNotFoundException();
        }

        private async Task<SubscriptionPlan> GetPlanAsync(int planId)
        {
            var repo = _unitOfWork.GetRepository<SubscriptionPlan, int>();
            var plan = await repo.GetByIdAsync(planId) ?? throw new PlanNotFoundException();
            if (plan.Status != EntityStatus.Active)
                throw new PlanNotActiveException();
            return plan;
        }

        private static decimal GetBaseAmount(SubscriptionPlan plan, BillingCycle cycle)
        {
            return cycle == BillingCycle.Monthly ? plan.PriceMonthly : plan.PricePerYear;
        }

        private async Task<(decimal finalAmount, decimal deductedAmount, double deductedPercent)> CalculateFinalAmountAsync(CreateSchoolSubscriptionCommand request, int schoolId, SubscriptionPlan plan, decimal baseAmount, DateTimeOffset now)
        {
            var repo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var existing = await repo.GetActiveSubscriptionBySchoolIdAsync(schoolId);
            if (existing == null)
                return (baseAmount, 0, 0);

            if (existing.PlanId == request.PlanId && existing.BillingCycle == request.BillingCycle)
                throw new SchoolSubscriptionAlreadyExistsException();

            if (IsDowngrade(request, plan, existing))
                throw new DowngradeNotAllowedException();

            var totalDays = existing.BillingCycle == BillingCycle.Monthly ? 30 : 365;
            var daysUsed = (now - existing.StartDate).TotalDays;
            var daysLeft = Math.Max(0, totalDays - daysUsed);
            var oldDailyRate = (double)existing.AmountPaid / totalDays;
            var deducted = (decimal)(daysLeft * oldDailyRate);
            var newPlanAmount = request.BillingCycle == BillingCycle.Monthly ? plan.PriceMonthly : plan.PricePerYear;
            var percent = (double)deducted / (double)newPlanAmount * 100;

            var final = baseAmount - deducted;
            if (final <= 10000) final = 10000;

            return (final, Math.Round(deducted, 2), Math.Round(percent, 2));
        }

        private static bool IsDowngrade(CreateSchoolSubscriptionCommand request, SubscriptionPlan newPlan, SchoolSubscription current)
        {
            return (current.BillingCycle == BillingCycle.Yearly && request.BillingCycle == BillingCycle.Monthly)
                || (request.BillingCycle == BillingCycle.Monthly && newPlan.PriceMonthly < current.Plan.PriceMonthly)
                || (request.BillingCycle == BillingCycle.Yearly && newPlan.PricePerYear < current.Plan.PricePerYear);
        }

        private static SchoolSubscription CreateNewSubscription(CreateSchoolSubscriptionCommand request, SubscriptionPlan plan, int schoolId, decimal amount, DateTimeOffset now, string transactionId)
        {
            return new SchoolSubscription
            {
                PlanId = plan.Id,
                SchoolId = schoolId,
                BillingCycle = request.BillingCycle,
                SubscriptionStatus = SubscriptionStatus.Peding,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.PayOS,
                TransactionId = transactionId,
                AmountPaid = amount,
                StartDate = now,
                EndDate = request.BillingCycle == BillingCycle.Monthly ? now.AddMonths(1) : now.AddYears(1),
                LastUsageResetDate = now,
                PurchasedAt = now,
                CurrentPeriodAIUsageMinutes = 0
            };
        }

        private async Task SaveSubscriptionAsync(SchoolSubscription subscription)
        {
            var repo = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            await repo.AddAsync(subscription);
            await _unitOfWork.CommitAsync();
        }

        private PaymentData BuildPaymentRequest(SubscriptionPlan plan, BillingCycle cycle, int amount, School school, long orderCode)
        {
            var billingCode = cycle == BillingCycle.Monthly ? "M" : "Y";
            return new PaymentData(
                orderCode: orderCode,
                amount: amount,
                description: $"{plan.Name}{billingCode}",
                items: new List<ItemData>
                {
                    new ItemData(name: plan.Name, quantity: 1, price: amount)
                },
                cancelUrl: _payOSConfig.CancelUrl,
                returnUrl: _payOSConfig.ReturnUrl,
                buyerName: school.Name,
                buyerEmail: school.ContactEmail,
                buyerPhone: school.ContactPhone
            );
        }
    }
}