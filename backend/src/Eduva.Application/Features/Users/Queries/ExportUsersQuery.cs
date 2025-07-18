using Eduva.Application.Features.Users.Requests;
using MediatR;

namespace Eduva.Application.Features.Users.Queries
{
    public record ExportUsersQuery(ExportUsersRequest Request, Guid SchoolAdminId) : IRequest<byte[]>;
}