using Eduva.Application.Features.Folders.Responses;
using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class MoveFolderCommand : IRequest<FolderResponse>
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public OwnerType OwnerType { get; set; }

        public Guid? ClassId { get; set; }

        [JsonIgnore]
        public Guid? UserId { get; set; }

        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
