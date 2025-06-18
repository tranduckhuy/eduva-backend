using Eduva.API.Controllers.Base;
using Eduva.Application.Common.Models;
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
        public async Task<IActionResult> GetSubscriptionPlans([FromQuery] SubscriptionPlanSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<SubscriptionPlanResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetSubscriptionPlansQuery(specParam));
                return (CustomCode.Success, result);
            });
        }
    }
}