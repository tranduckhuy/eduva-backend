using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Jobs.Commands.DeleteCompletedJob
{
    public class DeleteCompletedJobCommand : IRequest<Unit>
    {
        public Guid JobId { get; set; }
        public bool Permanent { get; set; } = false;

        [JsonIgnore]
        public Guid UserId { get; set; }
    }
}
