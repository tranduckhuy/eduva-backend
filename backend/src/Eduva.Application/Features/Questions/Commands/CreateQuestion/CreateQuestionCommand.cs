using Eduva.Application.Features.Questions.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommand : IRequest<QuestionResponse>
    {
        public Guid LessonMaterialId { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;

        [JsonIgnore]
        public Guid CreatedByUserId { get; set; }
    }
}