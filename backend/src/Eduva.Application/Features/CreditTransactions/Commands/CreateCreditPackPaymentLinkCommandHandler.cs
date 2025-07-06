using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Exceptions.CreditTransaction;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Constants;
using Eduva.Shared.Enums;
using MediatR;
using Net.payOS.Types;

namespace Eduva.Application.Features.CreditTransactions.Commands
{
    public class CreateCreditPackPaymentLinkCommandHandler
        : IRequestHandler<CreateCreditPackPaymentLinkCommand, (CustomCode, CreateCreditPackPaymentLinkResponse)>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayOSService _payOSService;
        private readonly ISystemConfigHelper _systemConfigHelper;

        public CreateCreditPackPaymentLinkCommandHandler(IUnitOfWork unitOfWork, IPayOSService payOSService, ISystemConfigHelper systemConfigHelper)
        {
            _unitOfWork = unitOfWork;
            _payOSService = payOSService;
            _systemConfigHelper = systemConfigHelper;
        }

        public async Task<(CustomCode, CreateCreditPackPaymentLinkResponse)> Handle(
            CreateCreditPackPaymentLinkCommand request, CancellationToken cancellationToken)
        {
            var creditPackRepo = _unitOfWork.GetRepository<AICreditPack, int>();
            var creditPack = await creditPackRepo.GetByIdAsync(request.CreditPackId) ?? throw new AICreditPackNotFoundException();

            if (creditPack.Status != EntityStatus.Active)
            {
                throw new AICreditPackNotActiveException();
            }

            var now = DateTimeOffset.UtcNow;
            var transactionCode = now.ToUnixTimeSeconds().ToString();

            var transaction = new PaymentTransaction
            {
                UserId = request.UserId,
                PaymentPurpose = PaymentPurpose.CreditPackage,
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                PaymentItemId = request.CreditPackId,
                Amount = creditPack.Price,
                TransactionCode = transactionCode,
                CreatedAt = now
            };

            var transactionRepo = _unitOfWork.GetRepository<PaymentTransaction, Guid>();
            await transactionRepo.AddAsync(transaction);
            await _unitOfWork.CommitAsync();

            var returnUrl = await _systemConfigHelper.GetValueAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK);

            var paymentData = new PaymentData(
                orderCode: long.Parse(transactionCode),
                amount: (int)creditPack.Price,
                description: $"{creditPack.Name}",
                items: new List<ItemData>
                {
                    new ItemData(name: creditPack.Name, quantity: 1, price: (int)creditPack.Price)
                },
                cancelUrl: returnUrl,
                returnUrl: returnUrl
            );

            var result = await _payOSService.CreatePaymentLinkAsync(paymentData);

            var response = new CreateCreditPackPaymentLinkResponse
            {
                CheckoutUrl = result.checkoutUrl,
                PaymentLinkId = result.paymentLinkId,
                Amount = (long)creditPack.Price,
                TransactionCode = transactionCode
            };

            return (CustomCode.Success, response);
        }
    }
}