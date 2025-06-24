using Eduva.Application.Features.Folders.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Folders.Commands
{
    public class UpdateFolderOrderCommand : IRequest<FolderResponse>
    {
        public int Id { get; set; }
        
        public int Order { get; set; }
        
        [JsonIgnore]
        public Guid CurrentUserId { get; set; }
    }
}
