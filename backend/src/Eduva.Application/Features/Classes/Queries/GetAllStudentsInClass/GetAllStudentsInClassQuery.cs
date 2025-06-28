using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass
{
    public record GetAllStudentsInClassQuery(Guid ClassId, StudentClassSpecParam StudentClassSpecParam, Guid RequesterId)
        : IRequest<Pagination<StudentClassResponse>>;
}
