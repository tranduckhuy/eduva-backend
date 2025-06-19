using Eduva.Application.Features.Classes.Responses;
using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands
{
    public class UpdateClassCommand : IRequest<ClassResponse>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid TeacherId { get; set; }

        public string Name { get; set; } = string.Empty;
        public EntityStatus Status { get; set; } = EntityStatus.Active;
    }
}
