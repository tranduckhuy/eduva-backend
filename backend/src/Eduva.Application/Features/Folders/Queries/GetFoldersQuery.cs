using Eduva.Application.Common.Models;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using MediatR;

namespace Eduva.Application.Features.Folders.Queries
{
    public record GetFoldersQuery(FolderSpecParam FolderSpecParam, Guid? UserId = null) : IRequest<Pagination<FolderResponse>>;
}
