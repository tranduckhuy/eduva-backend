namespace Eduva.Application.Features.CreditTransactions.Responses
{
    public class CreateCreditPackPaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string? TransactionCode { get; set; }
    }
}
