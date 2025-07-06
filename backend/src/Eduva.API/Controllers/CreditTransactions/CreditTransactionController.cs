using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.CreditTransactions.Commands;
using Eduva.Application.Features.CreditTransactions.Queries;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Features.CreditTransactions.Specifications;
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

        [HttpGet]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<Pagination<CreditTransactionResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserCreditTransactions([FromQuery] CreditTransactionSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<CreditTransactionResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetCreditTransactionQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.Student)}")]
        [ProducesResponseType(typeof(ApiResponse<CreditTransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCreditTransactionById(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync<CreditTransactionResponse>(async () =>
            {
                var result = await _mediator.Send(new GetCreditTransactionByIdQuery(id, userId, User.IsInRole(nameof(Role.SystemAdmin))));
                return (CustomCode.Success, result);
            });
        }

        [HttpPost("payment-link")]
        [Authorize(Roles = $"{nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}")]
        [ProducesResponseType(typeof(ApiResponse<CreateCreditPackPaymentLinkResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreateCreditPackPaymentLinkCommand command)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.UserId = userId;

            return await HandleRequestAsync(() => _mediator.Send(command));
        }
    }
}