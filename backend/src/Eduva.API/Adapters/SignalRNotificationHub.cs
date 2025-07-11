using Eduva.API.Hubs;
using Eduva.Application.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Eduva.API.Adapters
{
    public class SignalRNotificationHub : INotificationHub
    {
        private readonly IHubContext<QuestionCommentHub> _questionCommentHubContext;
        private readonly IHubContext<JobStatusHub> _jobStatusHubContext;

        public SignalRNotificationHub(
            IHubContext<QuestionCommentHub> questionCommentHubContext,
            IHubContext<JobStatusHub> jobStatusHubContext)
        {
            _questionCommentHubContext = questionCommentHubContext;
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
                await _questionCommentHubContext.Clients.Group(groupName).SendAsync(eventName, data);
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
                await _questionCommentHubContext.Clients.User(userId).SendAsync(eventName, data);
            }
        }

        public async Task SendNotificationToAllAsync(string eventName, object data)
        {
            // For broadcast notifications, send to both hubs
            await _questionCommentHubContext.Clients.All.SendAsync(eventName, data);
        }
    }
}