using Eduva.Application.Common.Helpers;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, Pagination<NotificationResponse>>
    {
        private readonly INotificationService _notificationService;

        public GetUserNotificationsQueryHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<Pagination<NotificationResponse>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
        {
            List<UserNotification> userNotifications;
            int totalCount;

            if (request.PageSize <= 0)
            {
                userNotifications = await _notificationService.GetUserNotificationsAsync(request.UserId, cancellationToken);
                totalCount = userNotifications.Count;
            }
            else
            {
                var skip = (request.PageIndex - 1) * request.PageSize;
                userNotifications = await _notificationService.GetUserNotificationsAsync(request.UserId, skip, request.PageSize, cancellationToken);
                totalCount = await _notificationService.GetTotalCountAsync(request.UserId, cancellationToken);
            }

            var items = userNotifications.Select(un => new NotificationResponse
            {
                Id = un.Id,
                Type = un.Notification.Type,
                Payload = NotificationPayloadHelper.DeserializePayload(un.Notification.Payload),
                CreatedAt = un.Notification.CreatedAt,
                IsRead = un.IsRead
            }).ToList();

            return new Pagination<NotificationResponse>
            {
                Data = items,
                Count = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
    }
}