using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.SubscriptionPlans.Commands.ActivatePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.ArchivePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.DeletePlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using Eduva.Application.Features.SubscriptionPlans.Queries;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.SubscriptionPlans.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.SubscriptionPlans
{
    [Route("api/subscription-plans")]
    [ApiController]
    public class SubscriptionPlanController : BaseController<SubscriptionPlanController>
    {
        private readonly IMediator _mediator;

        public SubscriptionPlanController(IMediator mediator, ILogger<SubscriptionPlanController> logger)
            : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [ProducesResponseType(typeof(ApiResponse<Pagination<SubscriptionPlanResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionPlans([FromQuery] SubscriptionPlanSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<SubscriptionPlanResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetSubscriptionPlansQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionPlanById(int id)
        {
            var query = new GetSubscriptionPlanByIdQuery(id);

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}/details")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanDetailResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSubscriptionPlanDetail(int id)
        {
            var query = new GetSubscriptionPlanDetailQuery(id);

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanCommand command)
        {
            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }


        [HttpPut("{id}")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSubscriptionPlan(int id, [FromBody] UpdateSubscriptionPlanCommand command)
        {
            command.Id = id;
            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ArchiveSubscriptionPlan(int id)
        {
            var command = new ArchiveSubscriptionPlanCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ActivateSubscriptionPlan(int id)
        {
            var command = new ActivateSubscriptionPlanCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSubscriptionPlan(int id)
        {
            var command = new DeleteSubscriptionPlanCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

    }
}