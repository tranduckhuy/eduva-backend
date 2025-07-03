using MediatR;

namespace Eduva.Application.Features.Classes.Commands.AddMaterialsToFolder
{
    public class AddMaterialsToFolderCommand : IRequest<bool>
    {
        public Guid ClassId { get; set; }
        public Guid FolderId { get; set; }
        public List<Guid> MaterialIds { get; set; } = new List<Guid>();
        public Guid CurrentUserId { get; set; }
    }
}