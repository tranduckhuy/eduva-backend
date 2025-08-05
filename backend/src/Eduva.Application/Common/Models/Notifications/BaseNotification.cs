namespace Eduva.Application.Common.Models.Notifications
{
    public abstract class BaseNotification
    {
        public Guid UserNotificationId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public string? PerformedByName { get; set; }
        public string? PerformedByAvatar { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByAvatar { get; set; }
        public string? CreatedByRole { get; set; }
    }
}