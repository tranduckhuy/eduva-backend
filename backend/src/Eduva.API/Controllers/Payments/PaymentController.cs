using Eduva.API.Controllers.Base;
using Eduva.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.Payments
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : BaseController<PaymentController>
    {
        private readonly IMediator _mediator;

        public PaymentController(IMediator mediator, ILogger<PaymentController> logger)
            : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSReturn([FromQuery] ConfirmPayOSPaymentReturnCommand query)
        {
            return await HandleRequestAsync(() => _mediator.Send(query));
        }
    }
}