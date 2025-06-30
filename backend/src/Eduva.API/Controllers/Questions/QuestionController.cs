using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Features.Questions.Commands.CreateQuestion;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.Questions
{
    [Route("api/questions")]
    public class QuestionController : BaseController<QuestionController>
    {
        private readonly IMediator _mediator;

        public QuestionController(IMediator mediator, ILogger<QuestionController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<CreateQuestionResponse>), StatusCodes.Status201Created)]
        [Authorize(Roles = $"{nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            command.CreatedByUserId = userGuid;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Created, result);
            });
        }
    }
}