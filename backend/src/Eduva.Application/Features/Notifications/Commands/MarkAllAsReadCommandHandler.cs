using Eduva.Application.Interfaces.Services;
using MediatR;

namespace Eduva.Application.Features.Notifications.Commands
{

    public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, bool>
    {
        private readonly INotificationService _notificationService;

        public MarkAllAsReadCommandHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            await _notificationService.MarkAllAsReadAsync(request.UserId, cancellationToken);
            return true;
        }
    }
}