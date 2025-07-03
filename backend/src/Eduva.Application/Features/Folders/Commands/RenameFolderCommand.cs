using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class RenameFolderCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
