using Eduva.Application.Common.Exceptions;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Notifications.Commands
{
    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, bool>
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public MarkAsReadCommandHandler(INotificationService notificationService, IUnitOfWork unitOfWork)
        {
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            var userNotificationRepo = _unitOfWork.GetCustomRepository<IUserNotificationRepository>();
            var userNotification = await userNotificationRepo.GetByIdAsync(request.UserNotificationId) ?? throw new AppException(CustomCode.NotificationNotFound);
            if (userNotification.TargetUserId != request.UserId)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            await _notificationService.MarkAsReadAsync(request.UserNotificationId, cancellationToken);
            return true;
        }
    }
}