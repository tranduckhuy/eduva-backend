using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Commands.CreateQuestion;
using Eduva.Application.Features.Questions.Commands.CreateQuestionComment;
using Eduva.Application.Features.Questions.Commands.DeleteQuestion;
using Eduva.Application.Features.Questions.Commands.DeleteQuestionComment;
using Eduva.Application.Features.Questions.Commands.UpdateQuestion;
using Eduva.Application.Features.Questions.Commands.UpdateQuestionComment;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
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

        [HttpGet("lesson/{lessonMaterialId:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<QuestionResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> GetQuestionsByLesson(Guid lessonMaterialId, [FromQuery] QuestionsByLessonSpecParam specParam)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync<Pagination<QuestionResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetQuestionsByLessonQuery(specParam, lessonMaterialId, currentUserId));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("my-questions")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<Pagination<QuestionResponse>>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}")]
        public async Task<IActionResult> GetMyQuestions([FromQuery] MyQuestionsSpecParam specParam)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync<Pagination<QuestionResponse>>(async () =>
            {
                var result = await _mediator.Send(new GetMyQuestionsQuery(specParam, userId));
                return (CustomCode.Success, result);
            });
        }

        [HttpGet("{id:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [ProducesResponseType(typeof(ApiResponse<QuestionDetailResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> GetQuestionDetail(Guid id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var currentUserId))
                return Respond(CustomCode.UserIdNotFound);

            return await HandleRequestAsync<QuestionDetailResponse>(async () =>
            {
                var result = await _mediator.Send(new GetQuestionDetailQuery(id, currentUserId));
                return (CustomCode.Success, result);
            });
        }

        [HttpPost]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status201Created)]
        [Authorize(Roles = $"{nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionCommand command)
        {
            var validationResult = ValidateCreateCommand(command, out var userId);
            if (validationResult != null)
            {
                return validationResult;
            }

            command.CreatedByUserId = userId;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Created, result);
            });
        }

        [HttpPut("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<QuestionResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            command.Id = id;
            command.UpdatedByUserId = userGuid;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpDelete("{id}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.Student)}")]
        public async Task<IActionResult> DeleteQuestion(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            var command = new DeleteQuestionCommand
            {
                Id = id,
                DeletedByUserId = userGuid
            };

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            }, CustomCode.Success);
        }

        [HttpPost("comments")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<QuestionCommentResponse>), StatusCodes.Status201Created)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> CreateQuestionComment([FromBody] CreateQuestionCommentCommand command)
        {
            var validationResult = ValidateCreateCommand(command, out var userId);
            if (validationResult != null)
            {
                return validationResult;
            }

            command.CreatedByUserId = userId;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Created, result);
            });
        }

        [HttpPut("comments/{commentId:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<QuestionCommentResponse>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> UpdateQuestionComment(Guid commentId, [FromBody] UpdateQuestionCommentCommand command)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            command.Id = commentId;
            command.UpdatedByUserId = userGuid;

            return await HandleRequestAsync(async () =>
            {
                var result = await _mediator.Send(command);
                return (CustomCode.Success, result);
            });
        }

        [HttpDelete("comments/{commentId:guid}")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [Authorize(Roles = $"{nameof(Role.Student)}, {nameof(Role.Teacher)}, {nameof(Role.ContentModerator)}, {nameof(Role.SchoolAdmin)}, {nameof(Role.SystemAdmin)}")]
        public async Task<IActionResult> DeleteQuestionComment(Guid commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var userGuid))
                return Respond(CustomCode.UserIdNotFound);

            var command = new DeleteQuestionCommentCommand
            {
                Id = commentId,
                DeletedByUserId = userGuid
            };

            return await HandleRequestAsync(async () =>
            {
                await _mediator.Send(command);
            }, CustomCode.Success);
        }

        #region Validation Helpers

        private IActionResult? ValidateCreateCommand<TCommand>(TCommand command, out Guid userId) where TCommand : class
        {
            userId = Guid.Empty;

            if (command == null)
            {
                return Respond(CustomCode.ModelInvalid);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out userId))
                return Respond(CustomCode.UserIdNotFound);

            return null;
        }

        #endregion

    }
}