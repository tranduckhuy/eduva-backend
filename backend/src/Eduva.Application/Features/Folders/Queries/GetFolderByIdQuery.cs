using Eduva.Application.Features.Folders.Responses;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetFolderByIdQuery : IRequest<FolderResponse>
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public GetFolderByIdQuery(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }
}
