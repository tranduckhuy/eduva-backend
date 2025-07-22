using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Mappings;
using Eduva.API.Models;
using Eduva.API.Models.LessonMaterials;
using Eduva.Application.Common.Constants;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands.ApproveLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.CreateLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.RestoreLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.UpdateLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Queries.GetAllLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovalsById;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialById;
using Eduva.Application.Features.LessonMaterials.Queries.GetOwnLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetPendingLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials;
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
    [Authorize]
    public class LessonMaterialController : BaseController<LessonMaterialController>
    {
        private readonly IMediator _mediator;

        public LessonMaterialController(IMediator mediator, ILogger<LessonMaterialController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLessonMaterials([FromQuery] Guid? classId = null, [FromQuery] Guid? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var schoolId = User.FindFirstValue(ClaimConstants.SchoolId);
            int? schoolIdInt = schoolId != null ? int.Parse(schoolId) : null;
            var isStudent = User.IsInRole(nameof(Role.Student));

            var query = new GetAllLessonMaterialsQuery(
                UserId: Guid.Parse(userId),
                IsStudent: isStudent,
                SchoolId: schoolIdInt,
                ClassId: classId,
                FolderId: folderId);

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Policy = "EducatorOnly")]
        public async Task<IActionResult> CreateLessonMaterial([FromBody] CreateLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            // Get schoolID from claims or context if needed
            var schoolId = int.Parse(User.FindFirstValue(ClaimConstants.SchoolId) ?? "0");

            command.SchoolId = schoolId > 0 ? schoolId : null;

            command.CreatedBy = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }


        [HttpGet("school-public")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        public async Task<IActionResult> GetSchoolPublicLessonMaterials([FromQuery] GetSchoolPublicLessonMaterialsRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int schoolIdInt = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : 0;

            if (schoolIdInt <= 0)
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            var lessonMaterialSpecParam = AppMapper<ModelMappingProfile>.Mapper.Map<LessonMaterialSpecParam>(request);
            lessonMaterialSpecParam.SchoolId = schoolIdInt;

            var query = new GetSchoolPublicLessonMaterialsQuery(lessonMaterialSpecParam, Guid.Parse(userId));

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        // get lesson material by id
        [HttpGet("{id:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLessonMaterialById(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int? schoolId = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : null;

            var query = new GetLessonMaterialByIdQuery
            {
                Id = id,
                UserId = Guid.Parse(userId),
                SchoolId = schoolId
            };
            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        [HttpGet("pending-approval")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [Authorize(Roles = $"{nameof(Role.SchoolAdmin)},{nameof(Role.ContentModerator)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> GetPendingLessonMaterials([FromQuery] GetPendingLessonMaterialsRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int schoolIdInt = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : 0;

            if (schoolIdInt <= 0)
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            // Get user roles
            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var lessonMaterialSpecParam = AppMapper<ModelMappingProfile>.Mapper.Map<LessonMaterialSpecParam>(request);
            lessonMaterialSpecParam.SchoolId = schoolIdInt;

            var query = new GetPendingLessonMaterialsQuery(lessonMaterialSpecParam, Guid.Parse(userId), userRoles);

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        [HttpPut("{id:guid}/pending-approval")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.ContentModerator)},{nameof(Role.SchoolAdmin)}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLessonMaterial(Guid id, [FromBody] ApproveLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.Id = id;
            command.ModeratorId = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }

        [HttpPut("{personalFolderId:guid}/restore")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Teacher)},{nameof(Role.ContentModerator)},{nameof(Role.SchoolAdmin)},{nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> RestoreLessonMaterials(Guid personalFolderId, [FromBody] List<Guid> materialIds)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
                return validationResult;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Respond(CustomCode.UserIdNotFound);

            var command = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = personalFolderId,
                LessonMaterialIds = materialIds,
                CurrentUserId = Guid.Parse(userId)
            };

            return await HandleRequestAsync<object>(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Policy = "EducatorOnly")]
        public async Task<IActionResult> UpdateLessonMaterial(Guid id, [FromBody] UpdateLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.Id = id;
            command.CreatorId = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }

        // Get own lesson materials by status with pagination
        [HttpGet("me")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [Authorize(Policy = "EducatorOnly")]
        public async Task<IActionResult> GetOwnLessonMaterialsByStatus([FromQuery] GetOwnLessonMaterialsRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int schoolIdInt = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : 0;
            if (schoolIdInt <= 0)
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            var lessonMaterialSpecParam = AppMapper<ModelMappingProfile>.Mapper.Map<LessonMaterialSpecParam>(request);
            lessonMaterialSpecParam.SchoolId = schoolIdInt;

            var query = new GetOwnLessonMaterialsQuery(lessonMaterialSpecParam, Guid.Parse(userId));
            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        // Delete lesson material by id
        [HttpDelete]
        [Authorize(Policy = "EducatorOnly")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteLessonMaterial([FromBody] DeleteLessonMaterialCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int schoolIdInt = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : 0;
            if (schoolIdInt <= 0)
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            command.SchoolId = schoolIdInt;
            command.UserId = Guid.Parse(userId);

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            });
        }

        [HttpGet("approvals")]
        [Authorize(Policy = "EducatorOnly")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        public async Task<IActionResult> GetLessonMaterialApprovals([FromQuery] GetLessonMaterialApprovalsRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            int? schoolId = int.TryParse(User.FindFirstValue(ClaimConstants.SchoolId), out var parsedSchoolId) ? parsedSchoolId : null;

            // Get user roles
            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var specParam = AppMapper<ModelMappingProfile>.Mapper.Map<LessonMaterialApprovalsSpecParam>(request);

            // Set school ID from claim
            if (schoolId.HasValue)
            {
                specParam.SchoolId = schoolId;
            }

            var query = new GetLessonMaterialApprovalsQuery(specParam, Guid.Parse(userId), userRoles);

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }

        [HttpGet("{lessonMaterialId:guid}/approvals")]
        [Authorize(Policy = "EducatorOnly")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        public async Task<IActionResult> GetLessonMaterialApprovalsById([FromRoute] Guid lessonMaterialId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Respond(CustomCode.UserIdNotFound);

            var query = new GetLessonMaterialApprovalsByIdQuery(lessonMaterialId, Guid.Parse(userId));

            return await HandleRequestAsync(async () =>
            {
                var response = await _mediator.Send(query);
                return (CustomCode.Success, response);
            });
        }
    }
}