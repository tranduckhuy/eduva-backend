using Eduva.Application.Features.Folders.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class RenameFolderCommand : IRequest<FolderResponse>
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
