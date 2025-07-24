using Eduva.API.Hubs;
using Eduva.Application.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Eduva.API.Adapters
{
    public class SignalRNotificationHub : INotificationHub
    {
        private readonly IHubContext<NotificationHub> _notificationHubContext;
        private readonly IHubContext<JobStatusHub> _jobStatusHubContext;

        public SignalRNotificationHub(
            IHubContext<NotificationHub> notificationHubContext,
            IHubContext<JobStatusHub> jobStatusHubContext)
        {
            _notificationHubContext = notificationHubContext;
            _jobStatusHubContext = jobStatusHubContext;
        }

        public async Task SendNotificationToGroupAsync(string groupName, string eventName, object data)
        {
            // Determine which hub to use based on group name pattern
            if (groupName.StartsWith("job_"))
            {
                await _jobStatusHubContext.Clients.Group(groupName).SendAsync(eventName, data);
            }
            else
            {
                await _notificationHubContext.Clients.Group(groupName).SendAsync(eventName, data);
            }
        }

        public async Task SendNotificationToUserAsync(string userId, string eventName, object data)
        {
            if (eventName.StartsWith("Job"))
            {
                await _jobStatusHubContext.Clients.User(userId).SendAsync(eventName, data);
            }
            else
            {
                await _notificationHubContext.Clients.User(userId).SendAsync(eventName, data);
            }
        }

        public async Task SendNotificationToAllAsync(string eventName, object data)
        {
            // For broadcast notifications, send to both hubs
            await _notificationHubContext.Clients.All.SendAsync(eventName, data);
        }
    }
}