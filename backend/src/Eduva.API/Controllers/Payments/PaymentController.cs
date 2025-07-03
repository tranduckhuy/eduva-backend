using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Payments.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<Pagination<PaymentResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentTransactions([FromQuery] PaymentSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<PaymentResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetPaymentTransactionsQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("my")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}")]
        [ProducesResponseType(typeof(ApiResponse<Pagination<PaymentResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyPayments([FromQuery] MyPaymentSpecParam specParam)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            specParam.UserId = userId;

            return await HandleRequestAsync<Pagination<PaymentResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetMyPaymentsQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSReturn([FromQuery] ConfirmPayOSPaymentReturnCommand query)
        {
            return await HandleRequestAsync(() => _mediator.Send(query));
        }
    }
}