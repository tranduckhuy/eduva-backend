using MediatR;

namespace Eduva.Application.Features.Notifications.Commands
{
    public class MarkAsReadCommand : IRequest<bool>
    {
        public Guid UserNotificationId { get; set; }
        public Guid UserId { get; set; }
    }
}