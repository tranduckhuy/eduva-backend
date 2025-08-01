using Eduva.Application.Features.Folders.Responses;
using Eduva.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class CreateFolderCommand : IRequest<FolderResponse>
    {
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public Guid UserId { get; set; }

        [JsonPropertyName("classId")]
        public string? ClassIdString { get; set; }

        [JsonIgnore]
        public Guid? ClassId { get; set; }

        [JsonIgnore]
        public OwnerType OwnerType { get; set; }

        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
