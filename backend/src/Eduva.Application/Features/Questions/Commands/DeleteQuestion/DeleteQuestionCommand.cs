using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionCommand : IRequest<bool>
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid DeletedByUserId { get; set; }
    }
}