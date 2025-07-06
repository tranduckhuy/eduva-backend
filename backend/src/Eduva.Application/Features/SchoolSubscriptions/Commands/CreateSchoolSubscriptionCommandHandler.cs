using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using MediatR;
using Net.payOS.Types;

namespace Eduva.Application.Features.SchoolSubscriptions.Commands
{
    public class CreateSchoolSubscriptionCommandHandler : IRequestHandler<CreateSchoolSubscriptionCommand, (CustomCode, CreatePaymentLinkResponse)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayOSService _payOSService;
        private readonly ISystemConfigHelper _systemConfigHelper;

        public CreateSchoolSubscriptionCommandHandler(IUnitOfWork unitOfWork, IPayOSService payOSService, ISystemConfigHelper systemConfigHelper)
        {
            _unitOfWork = unitOfWork;
            _payOSService = payOSService;
            _systemConfigHelper = systemConfigHelper;
        }

        public async Task<(CustomCode, CreatePaymentLinkResponse)> Handle(CreateSchoolSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var user = await userRepo.GetByIdAsync(request.UserId) ?? throw new AppException(CustomCode.UserNotFound);
            if (user.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            request.SchoolId = user.SchoolId.Value;

            var school = await GetSchoolAsync(request.SchoolId);
            var plan = await GetPlanAsync(request.PlanId);
            var baseAmount = GetBaseAmount(plan, request.BillingCycle);

            var now = DateTimeOffset.UtcNow;
            var transactionCode = now.ToUnixTimeSeconds().ToString();

            var (finalAmount, deductedAmount, deductedPercent) = await CalculateFinalAmountAsync(request, school.Id, plan, baseAmount, now);

            var paymentTransaction = new PaymentTransaction
            {
                UserId = request.UserId,
                PaymentPurpose = PaymentPurpose.SchoolSubscription,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                PaymentItemId = request.PlanId,
                Amount = finalAmount,
                TransactionCode = transactionCode,
                CreatedAt = now
            };

            var transactionRepo = _unitOfWork.GetRepository<PaymentTransaction, Guid>();
            await transactionRepo.AddAsync(paymentTransaction);
            await _unitOfWork.CommitAsync();

            var paymentRequest = await BuildPaymentRequestAsync(plan, request.BillingCycle, (int)finalAmount, school, long.Parse(transactionCode));
            var result = await _payOSService.CreatePaymentLinkAsync(paymentRequest);

            var response = new CreatePaymentLinkResponse
            {
                CheckoutUrl = result.checkoutUrl,
                PaymentLinkId = result.paymentLinkId,
                Amount = (long)finalAmount,
                DeductedAmount = deductedAmount,
                DeductedPercent = deductedPercent,
                TransactionCode = transactionCode,
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
            {
                throw new PlanNotActiveException();
            }
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
            {
                return (baseAmount, 0, 0);
            }

            if (existing.PlanId == request.PlanId && existing.BillingCycle == request.BillingCycle)
            {
                throw new SchoolSubscriptionAlreadyExistsException();
            }

            if (IsDowngrade(request, plan, existing))
            {
                throw new DowngradeNotAllowedException();
            }

            var transactionRepo = _unitOfWork.GetRepository<PaymentTransaction, Guid>();
            var transaction = await transactionRepo.GetByIdAsync(existing.PaymentTransactionId) ?? throw new PaymentTransactionNotFoundException();

            var totalDays = existing.BillingCycle == BillingCycle.Monthly ? 30 : 365;
            var daysUsed = (now - existing.StartDate).TotalDays;
            var daysLeft = Math.Max(0, totalDays - daysUsed);
            var oldDailyRate = (double)transaction.Amount / totalDays;
            var deducted = (decimal)(daysLeft * oldDailyRate);
            var newPlanAmount = request.BillingCycle == BillingCycle.Monthly ? plan.PriceMonthly : plan.PricePerYear;
            var percent = (double)deducted / (double)newPlanAmount * 100;

            var final = baseAmount - deducted;

            if (final <= 10000)
            {
                final = 10000;
            }

            return (final, Math.Round(deducted, 2), Math.Round(percent, 2));
        }

        private static bool IsDowngrade(CreateSchoolSubscriptionCommand request, SubscriptionPlan newPlan, SchoolSubscription current)
        {
            return current.BillingCycle == BillingCycle.Yearly && request.BillingCycle == BillingCycle.Monthly
                || request.BillingCycle == BillingCycle.Monthly && newPlan.PriceMonthly < current.Plan.PriceMonthly
                || request.BillingCycle == BillingCycle.Yearly && newPlan.PricePerYear < current.Plan.PricePerYear;
        }

        private async Task<PaymentData> BuildPaymentRequestAsync(SubscriptionPlan plan, BillingCycle cycle, int amount, School school, long orderCode)
        {
            var billingCode = cycle == BillingCycle.Monthly ? " Thang" : " Nam";
            var returnUrl = await _systemConfigHelper.GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN);

            return new PaymentData(
                orderCode: orderCode,
                amount: amount,
                description: $"{plan.Name}{billingCode}",
                items: new List<ItemData>
                {
                    new ItemData(name: plan.Name, quantity: 1, price: amount)
                },
                cancelUrl: returnUrl,
                returnUrl: returnUrl,
                buyerName: school.Name,
                buyerEmail: school.ContactEmail,
                buyerPhone: school.ContactPhone
            );
        }
    }
}