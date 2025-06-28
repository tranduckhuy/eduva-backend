using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Folders
{
    [Route("api/folders")]
    public class FoldersController : BaseController<FoldersController>
    {
        private readonly IMediator _mediator;

        public FoldersController(IMediator mediator, ILogger<FoldersController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderCommand command)
        {
            try
            {
                var validationResult = CheckModelStateValidity();
                if (validationResult != null)
                {
                    return validationResult;
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                {
                    return Respond(CustomCode.UserIdNotFound);
                }

                if (string.IsNullOrWhiteSpace(command.ClassIdString))
                {
                    command.ClassId = null;
                }
                else
                {
                    if (Guid.TryParse(command.ClassIdString, out var classId))
                    {
                        command.ClassId = classId;
                    }
                    else
                    {
                        return Respond(CustomCode.ProvidedInformationIsInValid);
                    }
                }

                command.CurrentUserId = currentUserId;

                return await HandleRequestAsync(async () =>
                {
                    var result = await _mediator.Send(command);
                    return (CustomCode.Created, result);
                });
            }
            catch (Exception)
            {
                return Respond(CustomCode.FolderCreateFailed);
            }
        }

        [HttpGet]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<FolderResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> GetFolders([FromQuery] FolderSpecParam folderSpecParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync(async () =>
            {
                var query = new GetFoldersQuery(folderSpecParam, userGuid);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("class/{classId}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<FolderResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> GetFoldersByClassId(Guid classId, [FromQuery] FolderSpecParam folderSpecParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            folderSpecParam.ClassId = classId;
            folderSpecParam.OwnerType = OwnerType.Class;

            return await HandleRequestAsync(async () =>
            {
                // Access control: Only SchoolAdmin, Teacher of the class, or Students enrolled in the class can view folders
                var query = new GetFoldersQuery(folderSpecParam, userGuid);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("user")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<FolderResponse>>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> GetUserFolders([FromQuery] FolderSpecParam folderSpecParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            folderSpecParam.UserId = userGuid;
            folderSpecParam.OwnerType = OwnerType.Personal;

            return await HandleRequestAsync(async () =>
            {
                var query = new GetFoldersQuery(folderSpecParam);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}/rename")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RenameFolder(Guid id, [FromBody] RenameFolderCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.Id = id;
            command.CurrentUserId = currentUserId;

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }

        [HttpPut("{id}/order")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateFolderOrder(Guid id, [FromBody] UpdateFolderOrderCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.Id = id;
            command.CurrentUserId = currentUserId;

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }
    }
}
