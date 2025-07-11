using Eduva.Application.Features.Payments.Responses;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.CreditTransactions.Responses
{
    public class CreditTransactionResponse
    {
        public Guid Id { get; set; }
        public int TotalCredits { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public UserInfo User { get; set; } = null!;
        public AICreditPackInfor AICreditPack { get; set; } = null!;
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid PaymentTransactionId { get; set; }

    }

    public class AICreditPackInfor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Credits { get; set; }
        public int BonusCredits { get; set; }
    }
}