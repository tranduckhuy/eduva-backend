using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Features.LessonMaterials.Responses;
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
    public class LessonMaterialController : BaseController<LessonMaterialController>
    {
        private readonly IMediator _mediator;

        public LessonMaterialController(IMediator mediator, ILogger<LessonMaterialController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<LessonMaterialResponse>), StatusCodes.Status200OK)]
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
                return Respond(CustomCode.SystemError, null,
                    [ex.InnerException?.Message ?? "The system encountered an unexpected error while processing the request"]);
            }
        }

        [HttpGet]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)},{nameof(Role.Student)}")]
        public async Task<IActionResult> GetLessonMaterials([FromQuery] LessonMaterialSpecParam lessonMaterialSpecParam)
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

                var query = new GetLessonMaterialsQuery(lessonMaterialSpecParam, Guid.Parse(userId));
                var response = await _mediator.Send(query);
                return Respond(CustomCode.Success, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving lesson materials");
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