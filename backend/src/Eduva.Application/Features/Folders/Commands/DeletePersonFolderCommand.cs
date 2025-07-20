using MediatR;

namespace Eduva.Application.Features.Folders.Commands
{
    public class DeletePersonFolderCommand : IRequest<bool>
    {
        public List<Guid> FolderIds { get; set; } = new();
        public Guid CurrentUserId { get; set; }
    }
}
