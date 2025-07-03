using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Commands.AddMaterialsToFolder;
using Eduva.Application.Features.Classes.Commands.ArchiveClass;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Commands.EnrollByClassCode;
using Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder;
using Eduva.Application.Features.Classes.Commands.RemoveStudentsFromClass;
using Eduva.Application.Features.Classes.Commands.ResetClassCode;
using Eduva.Application.Features.Classes.Commands.RestoreClass;
using Eduva.Application.Features.Classes.Commands.UpdateClass;
using Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass;
using Eduva.Application.Features.Classes.Queries.GetClassById;
using Eduva.Application.Features.Classes.Queries.GetClasses;
using Eduva.Application.Features.Classes.Queries.GetStudentById;
using Eduva.Application.Features.Classes.Queries.GetStudentClasses;
using Eduva.Application.Features.Classes.Queries.GetTeacherClasses;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
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
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status201Created)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassCommand command)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            command.TeacherId = Guid.Parse(userId);

            var schoolId = User.FindFirstValue("SchoolId");
            if (!string.IsNullOrEmpty(schoolId) && command.SchoolId == 0)
            {
                command.SchoolId = int.Parse(schoolId);
            }

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Created, result);
            });
        }

        [HttpGet]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<ClassResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> GetClasses([FromQuery] ClassSpecParam classSpecParam)
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
                var query = new GetClassesQuery(classSpecParam, userGuid);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("teaching")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<ClassResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Teacher)}")]
        public async Task<IActionResult> GetTeacherClasses([FromQuery] ClassSpecParam classSpecParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var teacherId))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync(async () =>
            {
                var query = new GetTeacherClassesQuery(classSpecParam, teacherId);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("enrollment")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<StudentClassResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}")]
        public async Task<IActionResult> GetMyClasses([FromQuery] StudentClassSpecParam specParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var studentId))
                return Respond(CustomCode.UserIdNotFound);

            // Create query with spec params
            var query = new GetStudentClassesQuery(specParam, studentId);

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        //Get class by ID
        [HttpGet("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> GetClassById(Guid id)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var requesterId))
                return Respond(CustomCode.UserIdNotFound);
            return await HandleRequestAsync(async () =>
            {
                var query = new GetClassByIdQuery(id, requesterId);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id}/students")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<StudentClassResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> GetAllStudentsInClass(Guid id, [FromQuery] StudentClassSpecParam studentClassSpecParam)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var requesterId))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync(async () =>
            {
                var query = new GetAllStudentsInClassQuery(id, studentClassSpecParam, requesterId);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("students/{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<StudentClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var requesterId))
                return Respond(CustomCode.UserIdNotFound);
            return await HandleRequestAsync(async () =>
            {
                var query = new GetStudentByIdQuery(id, requesterId);
                var result = await _mediator.Send(query);
                return (CustomCode.Success, result);
            });
        }

        [HttpPut("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] UpdateClassCommand command)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            command.Id = id;
            command.TeacherId = currentUserId;

            return await HandleRequestAsync(async () => await _mediator.Send(command));
        }

        [HttpPut("{id}/archive")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> ArchiveClass(Guid id)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            var command = new ArchiveClassCommand
            {
                Id = id,
                TeacherId = currentUserId
            }; try
            {
                await _mediator.Send(command);
                return Respond(CustomCode.Success);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, null, ex.Errors);
            }
            catch (Exception)
            {
                return Respond(CustomCode.SystemError);
            }
        }

        [HttpPut("{id}/restore")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> RestoreClass(Guid id)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            var command = new RestoreClassCommand
            {
                Id = id,
                TeacherId = currentUserId
            }; try
            {
                await _mediator.Send(command);
                return Respond(CustomCode.Success);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, null, ex.Errors);
            }
            catch (Exception)
            {
                return Respond(CustomCode.SystemError);
            }
        }

        [HttpPost("{id}/reset-code")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<ClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> ResetClassCode(Guid id)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

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

        [HttpPost("enroll-by-code")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<StudentClassResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}")]
        public async Task<IActionResult> EnrollByClassCode([FromBody] EnrollByClassCodeCommand command)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var studentId))
                return Respond(CustomCode.UserIdNotFound);

            command.StudentId = studentId;

            try
            {
                var result = await _mediator.Send(command);
                return Respond(CustomCode.Success, result);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, null, ex.Errors);
            }
            catch (Exception)
            {
                return Respond(CustomCode.SystemError);
            }
        }

        [HttpDelete("{classId}/students")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> RemoveStudentsFromClass(Guid classId, [FromBody] List<Guid> studentIds)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            // Determine user roles
            bool isTeacher = User.IsInRole(nameof(Role.Teacher));
            bool isSchoolAdmin = User.IsInRole(nameof(Role.SchoolAdmin));
            bool isSystemAdmin = User.IsInRole(nameof(Role.SystemAdmin));

            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = studentIds,
                RequestUserId = currentUserId,
                IsTeacher = isTeacher,
                IsSchoolAdmin = isSchoolAdmin,
                IsSystemAdmin = isSystemAdmin
            };

            try
            {
                await _mediator.Send(command);
                return Respond(CustomCode.Success);
            }
            catch (AppException ex)
            {
                return Respond(ex.StatusCode, null, ex.Errors);
            }
            catch (Exception)
            {
                return Respond(CustomCode.SystemError);
            }
        }

        [HttpPost("{classId}/folders/{folderId}/lesson-materials")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> AddMaterialsToFolder(Guid classId, Guid folderId, [FromBody] List<Guid> materialIds)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            var command = new AddMaterialsToFolderCommand
            {
                ClassId = classId,
                FolderId = folderId,
                MaterialIds = materialIds,
                CurrentUserId = currentUserId
            };

            return await HandleRequestAsync<object>(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpDelete("{classId}/folders/{folderId}/lesson-materials")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)},{nameof(Role.Teacher)}")]
        public async Task<IActionResult> RemoveMaterialsFromFolder(Guid classId, Guid folderId, [FromBody] List<Guid> materialIds)
        {
            var validationResult = CheckModelStateValidity();
            if (validationResult != null)
            {
                return validationResult;
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);
            var command = new RemoveMaterialsFromFolderCommand
            {
                ClassId = classId,
                FolderId = folderId,
                MaterialIds = materialIds,
                CurrentUserId = currentUserId
            };
            return await HandleRequestAsync<object>(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

    }
}
