using Eduva.Application.Common.Helpers;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Application.Interfaces.Services;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetNotificationSummaryQueryHandler : IRequestHandler<GetNotificationSummaryQuery, NotificationSummaryResponse>
    {
        private readonly INotificationService _notificationService;

        public GetNotificationSummaryQueryHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<NotificationSummaryResponse> Handle(GetNotificationSummaryQuery request, CancellationToken cancellationToken)
        {
            var unreadCount = await _notificationService.GetUnreadCountAsync(request.UserId, cancellationToken);
            var skip = 0;
            var take = 5;
            var recentNotifications = await _notificationService.GetUserNotificationsAsync(request.UserId, skip, take, cancellationToken);

            return new NotificationSummaryResponse
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications.Select(un => new NotificationResponse
                {
                    Id = un.Id,
                    Type = un.Notification.Type,
                    Payload = NotificationPayloadHelper.DeserializePayload(un.Notification.Payload),
                    CreatedAt = un.Notification.CreatedAt,
                    IsRead = un.IsRead
                }).ToList()
            };
        }
    }
}