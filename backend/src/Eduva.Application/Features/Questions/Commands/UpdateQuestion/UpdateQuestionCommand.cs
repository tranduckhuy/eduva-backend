using Eduva.Application.Features.Questions.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionCommand : IRequest<QuestionResponse>
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;

        [JsonIgnore]
        public Guid UpdatedByUserId { get; set; }
    }
}