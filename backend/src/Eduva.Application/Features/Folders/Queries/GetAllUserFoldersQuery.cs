using Eduva.Application.Features.Folders.Responses;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public record GetAllUserFoldersQuery(Guid UserId) : IRequest<List<FolderResponse>>;
}
