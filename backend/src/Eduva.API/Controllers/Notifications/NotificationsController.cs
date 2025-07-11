using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Notifications.Commands;
using Eduva.Application.Features.Notifications.Queries;
using Eduva.Application.Features.Notifications.Responses;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Notifications
{
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : BaseController<NotificationsController>
    {
        private readonly IMediator _mediator;

        public NotificationsController(ILogger<NotificationsController> logger, IMediator mediator)
            : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<Pagination<NotificationResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications([FromQuery] int? pageIndex, [FromQuery] int? pageSize)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandlePaginatedRequestAsync(async () =>
            {
                var query = new GetUserNotificationsQuery
                {
                    UserId = userGuid
                };

                if (pageIndex.HasValue)
                {
                    query.PageIndex = pageIndex.Value;
                }

                if (pageSize.HasValue)
                {
                    query.PageSize = pageSize.Value;
                }

                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("unread")]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var query = new GetUnreadNotificationsQuery { UserId = userGuid };
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<NotificationSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var query = new GetNotificationSummaryQuery { UserId = userGuid };
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{userNotificationId:guid}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAsRead(Guid userNotificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var command = new MarkAsReadCommand
                {
                    UserNotificationId = userNotificationId,
                    UserId = userGuid
                };

                await _mediator.Send(command);
            });
        }

        [HttpPut("read-all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var command = new MarkAllAsReadCommand { UserId = userGuid };
                await _mediator.Send(command);
            });
        }
    }
}