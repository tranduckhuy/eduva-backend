using Eduva.API.Controllers.Base;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands;
using Eduva.Application.Features.Classes.Queries;
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
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
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

            try
            {
                command.TeacherId = Guid.Parse(userId);
                var response = await _mediator.Send(command);
                return Respond(CustomCode.Success, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating class");

                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond(CustomCode.SystemError, null,
                    [ex.InnerException?.Message ?? "The system encountered an unexpected error while processing the request"]);
            }
        }

        [HttpGet]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)},{nameof(Role.Student)}")]
        public async Task<IActionResult> GetClasses([FromQuery] ClassSpecParam classSpecParam)
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
                var query = new GetClassesQuery(classSpecParam, Guid.Parse(userId));
                var response = await _mediator.Send(query);
                return Respond(CustomCode.Success, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving classes");
                if (ex is AppException appEx)
                {
                    return Respond(appEx.StatusCode, null, appEx.Errors);
                }
                return Respond(CustomCode.SystemError, null,
                    [ex.InnerException?.Message ?? "The system encountered an unexpected error while processing the request"]);
            }
        }
    }
}
