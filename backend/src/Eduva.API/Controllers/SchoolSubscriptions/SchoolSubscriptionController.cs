using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
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

        [HttpGet]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<Pagination<SchoolSubscriptionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolSubscriptions([FromQuery] SchoolSubscriptionSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<SchoolSubscriptionResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetSchoolSubscriptionQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<SchoolSubscriptionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolSubscriptionById(Guid id)
        {
            return await HandleRequestAsync<SchoolSubscriptionResponse>(async () =>
            {
                var result = await _mediator.Send(new GetSchoolSubscriptionByIdQuery(id));
                return (CustomCode.Success, result);
            });
        }

        [HttpPost("payment-link")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentLinkResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreateSchoolSubscriptionCommand command)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.UserId = userId;

            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpGet("current")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<MySchoolSubscriptionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetMySchoolSubscriptionQuery(userId);

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }
    }
}