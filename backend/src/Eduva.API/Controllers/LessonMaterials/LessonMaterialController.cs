using Eduva.API.Controllers.Base;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.LessonMaterials.Commands;
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
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> CreateLessonMaterial([FromBody] CreateLessonMaterialCommand command)
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

            try
            {
                command.CreatedBy = Guid.Parse(userId);
                var response = await _mediator.Send(command);
                return Respond(CustomCode.Success, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating lesson material");

                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond((CustomCode)StatusCodes.Status500InternalServerError, null,
                    [ex.InnerException?.Message ?? "The system encountered an unexpected error while processing the request"]);
            }
        }
    }
}
