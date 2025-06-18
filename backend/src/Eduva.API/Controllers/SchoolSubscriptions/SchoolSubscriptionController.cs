using Eduva.API.Controllers.Base;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("create-payment-link")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreateSchoolSubscriptionCommand command)
        {
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpGet("payos-return")]
        public async Task<IActionResult> PayOSReturn([FromQuery] ConfirmPayOSPaymentReturnCommand query)
        {
            return await HandleRequestAsync(() => _mediator.Send(query));
        }
    }
}