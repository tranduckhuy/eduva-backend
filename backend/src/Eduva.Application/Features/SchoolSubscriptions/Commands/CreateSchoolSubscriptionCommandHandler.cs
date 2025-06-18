using Eduva.Application.Exceptions.School;
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

            var subscription = new SchoolSubscription
            {
                PlanId = plan.Id,
                SchoolId = school.Id,
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

            var paymentRequest = new PaymentData(
                orderCode: orderCode,
                amount: (int)amount,
               description: $"{plan.Name}{(request.BillingCycle == BillingCycle.Monthly ? "M" : "Y")}",
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
                Amount = (long)amount
            };

            return (CustomCode.Success, response);
        }
    }
}