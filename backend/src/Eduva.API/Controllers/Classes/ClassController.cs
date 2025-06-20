using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Commands;
using Eduva.Application.Features.Classes.Queries;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Classes
{
    [Route("api/classes")]
    public class ClassController : BaseController<ClassController>
    {
        private readonly IMediator _mediator;

        public ClassController(IMediator mediator, ILogger<ClassController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status201Created)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            command.TeacherId = currentUserId;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Created, result);
            });
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<Pagination<ClassResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> GetClasses([FromQuery] ClassSpecParam classSpecParam)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync(async () =>
            {
                var query = new GetClassesQuery(classSpecParam, userGuid);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            command.Id = id;
            command.TeacherId = currentUserId;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);
            var command = new DeleteClassCommand
            {
                Id = id,
                TeacherId = currentUserId
            };

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpPost("{id}/reset-code")]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> ResetClassCode(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            var command = new ResetClassCodeCommand
            {
                Id = id,
                TeacherId = currentUserId
            };

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }
    }
}
