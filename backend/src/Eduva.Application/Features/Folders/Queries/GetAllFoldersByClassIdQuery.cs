using Eduva.Application.Features.Folders.Responses;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetAllFoldersByClassIdQuery : IRequest<IEnumerable<FolderResponse>>
    {
        public Guid ClassId { get; }
        public Guid UserId { get; }

        public GetAllFoldersByClassIdQuery(Guid classId, Guid userId)
        {
            ClassId = classId;
            UserId = userId;
        }
    }
}