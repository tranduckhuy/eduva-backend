using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
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
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateLessonMaterial([FromBody] CreateLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            // Get schoolID from claims or context if needed
            var schoolId = int.Parse(User.FindFirstValue("SchoolId") ?? "0");
            command.SchoolId = schoolId > 0 ? schoolId : null;

            command.CreatedBy = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });

        }

        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)},{nameof(Role.Student)}")]
        public async Task<IActionResult> GetLessonMaterials([FromQuery] LessonMaterialSpecParam lessonMaterialSpecParam)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var schoolId = User.FindFirstValue("SchoolId");
            if (schoolId != null)
            {
                lessonMaterialSpecParam.SchoolId = int.Parse(schoolId);
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