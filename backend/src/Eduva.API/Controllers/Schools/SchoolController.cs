using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Commands.ActivateSchool;
using Eduva.Application.Features.Schools.Commands.ArchiveSchool;
using Eduva.Application.Features.Schools.Commands.CreateSchool;
using Eduva.Application.Features.Schools.Commands.UpdateSchool;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Schools.Specifications;
using Eduva.Application.Features.Users.Responses;
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

        [HttpGet]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<Pagination<SchoolResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchools([FromQuery] SchoolSpecParam specParam)
        {
            return await HandleRequestAsync<Pagination<SchoolResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetSchoolQuery(specParam));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<SchoolDetailResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolById(int id)
        {
            return await HandleRequestAsync<SchoolDetailResponse>(async () =>
            {
                var result = await _mediator.Send(new GetSchoolByIdQuery(id));
                return (CustomCode.Success, result);
            });
        }

        // Get school user information by ID (SchoolAdmin only)
        [HttpGet("users/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        public async Task<IActionResult> GetSchoolUserByIdAsync(Guid userId)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var query = new GetUserByIdForSchoolAdminQuery(currentUserId, userId);
            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("limit")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<SchoolUserLimitResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSchoolUserLimit()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(new GetSchoolUserLimitQuery(userId));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("current")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<SchoolDetailResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentSchool()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var schoolAdminId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            return await HandleRequestAsync<SchoolDetailResponse>(async () =>
            {
                var result = await _mediator.Send(new GetMySchoolQuery(schoolAdminId));
                return (CustomCode.Success, result);
            });
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var schoolAdminId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.SchoolAdminId = schoolAdminId;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = nameof(Role.SchoolAdmin))]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSchool(int id, [FromBody] UpdateSchoolCommand command)
        {
            command.Id = id;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/archive")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ArchiveSchool(int id)
        {
            var command = new ArchiveSchoolCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }

        [HttpPut("{id}/activate")]
        [Authorize(Roles = nameof(Role.SystemAdmin))]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ActivateSchool(int id)
        {
            var command = new ActivateSchoolCommand(id);
            return await HandleRequestAsync(() => _mediator.Send(command));
        }
    }
}