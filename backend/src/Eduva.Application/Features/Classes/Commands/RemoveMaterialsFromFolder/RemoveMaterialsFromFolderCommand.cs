using MediatR;

namespace Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder
{
    public class RemoveMaterialsFromFolderCommand : IRequest<bool>
    {
        public Guid ClassId { get; set; }
        public Guid FolderId { get; set; }
        public List<Guid>? MaterialIds { get; set; }
        public Guid CurrentUserId { get; set; }
    }
}