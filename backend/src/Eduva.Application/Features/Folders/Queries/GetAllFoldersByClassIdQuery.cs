using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetAllFoldersByClassIdQuery : IRequest<IEnumerable<FolderResponse>>
    {
        public FolderSpecParam FolderSpecParam { get; }
        public Guid UserId { get; }

        public GetAllFoldersByClassIdQuery(FolderSpecParam folderSpecParam, Guid userId)
        {
            FolderSpecParam = folderSpecParam;
            UserId = userId;
        }
    }
}