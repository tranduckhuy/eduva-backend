using Eduva.API.Controllers.Base;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.LessonMaterials
{
    [Route("api/lesson-materials")]
    public class LessonMaterialController : BaseController<LessonMaterialController>
    {
        private readonly IMediator _mediator;

        public LessonMaterialController(IMediator mediator, ILogger<LessonMaterialController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateLessonMaterial([FromBody] CreateLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.CreatedBy = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(command);
                return (CustomCode.Success, response);
            });

        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetLessonMaterials([FromQuery] LessonMaterialSpecParam lessonMaterialSpecParam)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetLessonMaterialsQuery(lessonMaterialSpecParam, Guid.Parse(userId));

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });

        }
    }
}