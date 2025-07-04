using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Payments.Responses
{
    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaymentItemId { get; set; }
        public string? RelatedId { get; set; }
        public PaymentPurpose PaymentPurpose { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public UserInfo User { get; set; } = null!;
    }

    public class UserInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}