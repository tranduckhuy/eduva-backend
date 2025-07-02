using Eduva.Application.Features.Questions.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestionComment
{
    public class UpdateQuestionCommentCommand : IRequest<QuestionCommentResponse>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public string Content { get; set; } = default!;

        [JsonIgnore]
        public Guid UpdatedByUserId { get; set; }
    }
}