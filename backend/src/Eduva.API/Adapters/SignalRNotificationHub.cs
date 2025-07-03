using Eduva.API.Hubs;
using Eduva.Application.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Eduva.API.Adapters
{
    public class SignalRNotificationHub : INotificationHub
    {
        private readonly IHubContext<QuestionCommentHub> _hubContext;

        public SignalRNotificationHub(IHubContext<QuestionCommentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToGroupAsync(string groupName, string eventName, object data)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(eventName, data);
        }

        public async Task SendNotificationToUserAsync(string userId, string eventName, object data)
        {
            await _hubContext.Clients.User(userId).SendAsync(eventName, data);
        }

        public async Task SendNotificationToAllAsync(string eventName, object data)
        {
            await _hubContext.Clients.All.SendAsync(eventName, data);
        }
    }
}