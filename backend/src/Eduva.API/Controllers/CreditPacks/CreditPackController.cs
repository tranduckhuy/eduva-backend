using Eduva.API.Controllers.Base;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.AICreditPacks.Commands.ActivateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.ArchiveCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.DeleteCreditPacks;
using Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks;
using Eduva.Application.Features.AICreditPacks.Queries;
using Eduva.Application.Features.AICreditPacks.Responses;
using Eduva.Application.Features.AICreditPacks.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.CreditPacks
{
    [Route("api/credit-packs")]
    [ApiController]
    public class CreditPackController : BaseController<CreditPackController>
    {
        private readonly IMediator _mediator;

        public CreditPackController(IMediator mediator, ILogger<CreditPackController> logger)
            : base(logger)
        {
            _mediator = mediator;
        }


        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}")]
        public async Task<IActionResult> GetAICreditPacks([FromQuery] AICreditPackSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<AICreditPackResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetAICreditPacksQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpPost]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> CreateAICreditPack([FromBody] CreateAICreditPackCommand command)
        {
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> UpdateAICreditPack(int id, [FromBody] UpdateAICreditPackCommand command)
        {
            command.Id = id;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> ArchiveAICreditPack(int id)
        {
            var command = new ArchiveAICreditPackCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> ActivateAICreditPack(int id)
        {
            var command = new ActivateAICreditPackCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> DeleteAICreditPack(int id)
        {
            var command = new DeleteAICreditPackCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }
    }
}