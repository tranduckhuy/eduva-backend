using Eduva.API.Controllers.Base;
using Eduva.Application.Features.Schools.Commands;
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
    }
}