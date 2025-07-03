using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestionComment
{
    public class DeleteQuestionCommentCommand : IRequest<bool>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid DeletedByUserId { get; set; }
    }
}