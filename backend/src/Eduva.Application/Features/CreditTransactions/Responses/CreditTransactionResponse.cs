using Eduva.Application.Features.Payments.Responses;

namespace Eduva.Application.Features.CreditTransactions.Responses
{
    public class CreditTransactionResponse
    {
        public Guid Id { get; set; }
        public int Credits { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public UserInfo User { get; set; } = null!;
        public AICreditPackInfor AICreditPack { get; set; } = null!;
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