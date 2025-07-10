using Eduva.Application.Common.Models;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Domain.Constants;
using MediatR;

namespace Eduva.Application.Features.Notifications.Queries
{
    public class GetUserNotificationsQuery : IRequest<Pagination<NotificationResponse>>
    {
        public Guid UserId { get; set; }
        public int PageIndex { get; set; } = AppConstants.DEFAULT_PAGE_INDEX;
        public int PageSize { get; set; } = AppConstants.DEFAULT_PAGE_SIZE;
    }
}