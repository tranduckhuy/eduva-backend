using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Queries.GetStudentClasses
{
    public record GetStudentClassesQuery(StudentClassSpecParam StudentClassSpecParam, Guid StudentId)
        : IRequest<Pagination<StudentClassResponse>>;
}
