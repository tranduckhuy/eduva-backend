using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class UpdateFolderOrderCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid Id { get; set; }

        public int Order { get; set; }

        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
