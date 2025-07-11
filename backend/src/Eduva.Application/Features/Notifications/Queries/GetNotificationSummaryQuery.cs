using Eduva.Application.Features.Notifications.Responses;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetNotificationSummaryQuery : IRequest<NotificationSummaryResponse>
    {
        public Guid UserId { get; set; }
    }
}