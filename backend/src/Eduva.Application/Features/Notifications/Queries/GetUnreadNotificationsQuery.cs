using Eduva.Application.Features.Notifications.Responses;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetUnreadNotificationsQuery : IRequest<List<NotificationResponse>>
    {
        public Guid UserId { get; set; }
    }
}