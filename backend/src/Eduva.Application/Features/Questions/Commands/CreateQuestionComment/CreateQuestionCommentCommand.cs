using Eduva.Application.Features.Questions.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestionComment
{
    public class CreateQuestionCommentCommand : IRequest<QuestionCommentResponse>
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = default!;

        public Guid? ParentCommentId { get; set; }

        [JsonIgnore]
        public Guid CreatedByUserId { get; set; }
    }
}