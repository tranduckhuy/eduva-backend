using Eduva.API.Controllers.Base;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.SchoolSubscriptions
{
    [Route("api/school-subscriptions")]
    public class SchoolSubscriptionController : BaseController<SchoolSubscriptionController>
    {
        private readonly IMediator _mediator;

        public SchoolSubscriptionController(IMediator mediator, ILogger<SchoolSubscriptionController> logger)
            : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost("payment-link")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreateSchoolSubscriptionCommand command)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Respond(CustomCode.UserIdNotFound);

            command.UserId = userId;

            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSReturn([FromQuery] ConfirmPayOSPaymentReturnCommand query)
        {
            return await HandleRequestAsync(() => _mediator.Send(query));
        }

        [HttpGet("current")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Respond(CustomCode.UserIdNotFound);

            var query = new GetMySchoolSubscriptionQuery(userId);

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }
    }
}