namespace Eduva.Application.Features.Notifications.Responses
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public object Payload { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class NotificationSummaryResponse
    {
        public int UnreadCount { get; set; }
        public List<NotificationResponse> RecentNotifications { get; set; } = new();
    }
}