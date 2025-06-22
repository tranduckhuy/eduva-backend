using Eduva.API.Controllers.Base;
using Eduva.Application.Features.CreditTransactions.Commands;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.CreditTransactions
{
    [Route("api/credit-transactions")]
    [ApiController]
    public class CreditTransactionController : BaseController<CreditTransactionController>
    {
        private readonly IMediator _mediator;

        public CreditTransactionController(IMediator mediator, ILogger<CreditTransactionController> logger)
            : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost("payment-link")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreateCreditPackPaymentLinkCommand command)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Respond(CustomCode.UserIdNotFound);

            command.UserId = userId;

            return await HandleRequestAsync(() => _mediator.Send(command));
        }
    }
}