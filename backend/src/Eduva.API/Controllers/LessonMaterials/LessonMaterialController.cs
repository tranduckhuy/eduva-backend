using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Mappings;
using Eduva.API.Models;
using Eduva.API.Models.LessonMaterials;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Queries.GetAllLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialById;
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
        private const string SCHOOL_ID_CLAIM = "SchoolId";
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

            var schoolId = User.FindFirstValue(SCHOOL_ID_CLAIM);
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
            var schoolId = int.Parse(User.FindFirstValue(SCHOOL_ID_CLAIM) ?? "0");

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

            int schoolIdInt = int.TryParse(User.FindFirstValue(SCHOOL_ID_CLAIM), out var parsedSchoolId) ? parsedSchoolId : 0;

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

            int? schoolId = int.TryParse(User.FindFirstValue(SCHOOL_ID_CLAIM), out var parsedSchoolId) ? parsedSchoolId : null;

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

            int schoolIdInt = int.TryParse(User.FindFirstValue(SCHOOL_ID_CLAIM), out var parsedSchoolId) ? parsedSchoolId : 0;

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
    }
}