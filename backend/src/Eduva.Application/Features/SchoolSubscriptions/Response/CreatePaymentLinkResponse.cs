namespace Eduva.Application.Features.SchoolSubscriptions.Response
{
    public class CreatePaymentLinkResponse
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public long Amount { get; set; }
    }
}