namespace Eduva.Application.Features.Users.Responses
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int? SchoolId { get; set; }
        public List<string> Roles { get; set; } = [];
        public int CreditBalance { get; set; }
        public bool Is2FAEnabled { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public UserSubscriptionResponse? UserSubscriptionResponse { get; set; }

    }

    public class UserSubscriptionResponse
    {
        public bool IsSubscriptionActive { get; set; }
        public DateTimeOffset SubscriptionEndDate { get; set; }
    }
}
