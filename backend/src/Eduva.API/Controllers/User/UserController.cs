using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.User
{
    [Route("api/users")]
    public class UserController : BaseController<UserController>
    {
        private readonly IMediator _mediator;

        public UserController(ILogger<UserController> logger, IMediator mediator) : base(logger)
        {
            _mediator = mediator;
        }

        // Get the current user information
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> GetUserProfileAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
                return Respond(CustomCode.UserIdNotFound);

            var query = new GetUserProfileQuery(UserId: id);

            return await HandleRequestAsync(async () =>
                {
                    var result = await _mediator.Send(query);
                    return (CustomCode.Success, result);
                }
            );
        }

        // Get user information by ID
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> GetUserByIdAsync(Guid userId)
        {
            var query = new GetUserProfileQuery(UserId: userId);
            return await HandleRequestAsync(async () =>
                {
                    var result = await _mediator.Send(query);
                    return (CustomCode.Success, result);
                }
            );
        }

        // Update user profile
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateUserProfileCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var id))
                return Respond(CustomCode.UserIdNotFound);

            command.UserId = id;
            return await HandleRequestAsync(() => _mediator.Send(command));
        }
    }
}
