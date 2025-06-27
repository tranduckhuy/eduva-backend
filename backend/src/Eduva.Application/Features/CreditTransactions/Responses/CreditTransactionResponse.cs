namespace Eduva.Application.Features.CreditTransactions.Responses
{
    public class CreditTransactionResponse
    {
        public Guid Id { get; set; }
        public int Credits { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public UserInfor User { get; set; } = null!;
        public AICreditPackInfor AICreditPack { get; set; } = null!;
        public Guid PaymentTransactionId { get; set; }
    }

    public class UserInfor
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
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