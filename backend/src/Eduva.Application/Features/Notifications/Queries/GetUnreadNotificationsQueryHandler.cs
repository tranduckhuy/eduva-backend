using Eduva.Application.Common.Helpers;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Application.Interfaces.Services;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, List<NotificationResponse>>
    {
        private readonly INotificationService _notificationService;

        public GetUnreadNotificationsQueryHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<List<NotificationResponse>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userNotifications = await _notificationService.GetUnreadNotificationsAsync(request.UserId, cancellationToken);

            return userNotifications.Select(un => new NotificationResponse
            {
                Id = un.Id,
                Type = un.Notification.Type,
                Payload = NotificationPayloadHelper.DeserializePayload(un.Notification.Payload),
                CreatedAt = un.Notification.CreatedAt,
                IsRead = un.IsRead
            }).ToList();
        }
    }
}