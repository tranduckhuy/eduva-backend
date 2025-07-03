using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands.ArchiveClass
{
    public class ArchiveClassCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid TeacherId { get; set; }
    }
}
