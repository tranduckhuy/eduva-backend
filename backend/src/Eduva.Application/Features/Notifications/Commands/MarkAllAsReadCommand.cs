using MediatR;

namespace Eduva.Application.Features.Notifications.Commands
{
    public class MarkAllAsReadCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
    }
}