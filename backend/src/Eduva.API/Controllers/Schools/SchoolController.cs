using Eduva.API.Controllers.Base;
using Eduva.Application.Features.Schools.Commands.ActivateSchool;
using Eduva.Application.Features.Schools.Commands.ArchiveSchool;
using Eduva.Application.Features.Schools.Commands.CreateSchool;
using Eduva.Application.Features.Schools.Commands.UpdateSchool;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Schools
{
    [Route("api/schools")]
    [ApiController]
    public class SchoolController : BaseController<SchoolController>
    {
        private readonly IMediator _mediator;

        public SchoolController(IMediator mediator, ILogger<SchoolController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var schoolAdminId))
                return Respond(CustomCode.UserIdNotFound);

            command.SchoolAdminId = schoolAdminId;

            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> UpdateSchool(int id, [FromBody] UpdateSchoolCommand command)
        {
            command.Id = id;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> ArchiveSchool(int id)
        {
            var command = new ArchiveSchoolCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> ActivateSchool(int id)
        {
            var command = new ActivateSchoolCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

    }
}