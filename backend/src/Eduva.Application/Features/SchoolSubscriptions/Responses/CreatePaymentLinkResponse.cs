namespace Eduva.Application.Features.Payments.Responses
{
    public class CreatePaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string? TransactionCode { get; set; }
        public double? DeductedPercent { get; set; }
    }
}