namespace Eduva.Application.Contracts.Hubs
{
    public interface INotificationHub
    {
        Task SendNotificationToGroupAsync(string groupName, string eventName, object data);
        Task SendNotificationToUserAsync(string userId, string eventName, object data);
        Task SendNotificationToAllAsync(string eventName, object data);
    }
}