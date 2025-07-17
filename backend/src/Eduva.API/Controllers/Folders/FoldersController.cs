using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Folders
{
    [Route("api/folders")]
    [Authorize]
    public class FoldersController : BaseController<FoldersController>
    {
        private readonly IMediator _mediator;
        private const string SCHOOL_ID_CLAIM = "SchoolId";

        public FoldersController(IMediator mediator, ILogger<FoldersController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Policy = "EducatorOnly")]
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
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FolderResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFoldersByClassId(Guid classId)
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
                var query = new GetAllFoldersByClassIdQuery(classId, userGuid);
                var folders = await _mediator.Send(query);
                return (CustomCode.Success, folders);
            });
        }

        [HttpGet("user")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<FolderResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<FolderResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserFolders(
            [FromQuery] FolderSpecParam folderSpecParam,
            [FromQuery] bool isPaging = true)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            if (!isPaging)
            {
                folderSpecParam.UserId = userGuid;
                folderSpecParam.OwnerType = OwnerType.Personal;
                var query = new GetAllUserFoldersQuery(folderSpecParam);
                var result = await _mediator.Send(query);
                return Respond(CustomCode.Success, result);
            }
            else
            {
                folderSpecParam.UserId = userGuid;
                folderSpecParam.OwnerType = OwnerType.Personal;
                var query = new GetFoldersQuery(folderSpecParam);
                var result = await _mediator.Send(query);
                return Respond(CustomCode.Success, result);
            }
        }

        //Get Folder by ID
        [HttpGet("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFolderById(Guid id)
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
                var query = new GetFolderByIdQuery(id, userGuid);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}/rename")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Policy = "EducatorOnly")]
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
        [Authorize(Policy = "EducatorOnly")]
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
        [HttpPut("{id}/archive")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Policy = "EducatorOnly")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ArchiveFolder(Guid id)
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

            var command = new ArchiveFolderCommand
            {
                Id = id,
                CurrentUserId = currentUserId
            };

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }
        [HttpPut("{id}/restore")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Policy = "EducatorOnly")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RestoreFolder(Guid id)
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

            var command = new RestoreFolderCommand
            {
                Id = id,
                CurrentUserId = currentUserId
            };

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }

        [HttpDelete("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Policy = "EducatorOnly")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteFolder(Guid id)
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
            var command = new DeleteFolderCommand
            {
                Id = id,
                CurrentUserId = currentUserId
            };

            try
            {
                var result = await _mediator.Send(command);
                if (result)
                {
                    return Respond(CustomCode.Deleted);
                }
                return Respond(CustomCode.FolderDeleteFailed);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, ex.Errors);
            }
            catch (Exception)
            {
                return Respond(CustomCode.FolderDeleteFailed);
            }
        }

        [HttpGet("{folderId:guid}/lesson-materials")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
        [ApiExplorerSettings(GroupName = "LessonMaterial")]
        public async Task<IActionResult> GetLessonMaterialsByFolder(
            Guid folderId,
            [FromQuery] LessonMaterialFilterOptions lessonMaterialFilterOptions)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int schoolId = int.TryParse(User.FindFirstValue(SCHOOL_ID_CLAIM), out var parsedSchoolId) ? parsedSchoolId : 0;

            if (schoolId <= 0)
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            // Get user roles
            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var query = new GetLessonMaterialsByFolderQuery(
                folderId,
                Guid.Parse(userId),
                schoolId,
                userRoles,
                lessonMaterialFilterOptions
            );

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }
    }
}
