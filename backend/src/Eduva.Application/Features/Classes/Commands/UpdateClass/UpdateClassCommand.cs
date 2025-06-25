using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands.UpdateClass
{
    public class UpdateClassCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid TeacherId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
