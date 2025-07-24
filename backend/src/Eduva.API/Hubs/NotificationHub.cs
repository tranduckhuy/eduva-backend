using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Eduva.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("[SignalR] User connected - ConnectionId: {ConnectionId}, " +
                "UserId: {UserId}",
                Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (exception != null)
            {
                _logger.LogWarning("[SignalR] User disconnected with error - ConnectionId: {ConnectionId}, " +
                    "UserId: {UserId}, Error: {ErrorMessage}",
                    Context.ConnectionId, userId, exception.Message);
            }
            else
            {
                _logger.LogInformation("[SignalR] User disconnected normally - ConnectionId: {ConnectionId}, " +
                    "UserId: {UserId}",
                    Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Hub Methods - Group Management

        public async Task JoinLessonGroup(string lessonMaterialId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = $"Lesson_{lessonMaterialId}";

            _logger.LogInformation("[SignalR] User joining group - ConnectionId: {ConnectionId}, " +
                "UserId: {UserId}, GroupName: {GroupName}, LessonId: {LessonId}",
                Context.ConnectionId, userId, groupName, lessonMaterialId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("[SignalR] User successfully joined group - ConnectionId: {ConnectionId}, " +
                "GroupName: {GroupName}",
                Context.ConnectionId, groupName);
        }

        public async Task LeaveLessonGroup(string lessonMaterialId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var groupName = $"Lesson_{lessonMaterialId}";

            _logger.LogInformation("[SignalR] User leaving group - ConnectionId: {ConnectionId}, " +
                "UserId: {UserId}, GroupName: {GroupName}, LessonId: {LessonId}",
                Context.ConnectionId, userId, groupName, lessonMaterialId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation("[SignalR] User successfully left group - ConnectionId: {ConnectionId}, " +
                "GroupName: {GroupName}",
                Context.ConnectionId, groupName);
        }

        #endregion

    }
}