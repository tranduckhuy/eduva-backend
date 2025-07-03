using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class ArchiveFolderCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
