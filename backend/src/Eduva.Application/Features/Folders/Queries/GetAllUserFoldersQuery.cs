using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public record GetAllUserFoldersQuery(FolderSpecParam FolderSpecParam) : IRequest<List<FolderResponse>>;
}
