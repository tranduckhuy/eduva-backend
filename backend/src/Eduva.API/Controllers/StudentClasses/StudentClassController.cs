using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.StudentClasses.Commands;
using Eduva.Application.Features.StudentClasses.Queries;
using Eduva.Application.Features.StudentClasses.Responses;
using Eduva.Application.Features.StudentClasses.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.StudentClasses
{
    [Route("api/student-classes")]
    public class StudentClassController : BaseController<StudentClassController>
    {
        private readonly IMediator _mediator;

        public StudentClassController(IMediator mediator, ILogger<StudentClassController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost("enroll-by-code")]
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student to class: {ErrorMessage}", ex.Message);
                return Respond(CustomCode.SystemError);
            }
        }

        [HttpGet("my-classes")]
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
    }
}
