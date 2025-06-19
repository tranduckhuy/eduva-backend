namespace Eduva.Application.Features.SchoolSubscriptions.Response
{
    public class CreatePaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public decimal? DeductedAmount { get; set; }
        public string? TransactionId { get; set; }
        public double? DeductedPercent { get; set; }
    }
}