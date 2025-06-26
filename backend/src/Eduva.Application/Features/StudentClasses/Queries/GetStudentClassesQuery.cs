using Eduva.Application.Common.Models;
using Eduva.Application.Features.StudentClasses.Responses;
using Eduva.Application.Features.StudentClasses.Specifications;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.StudentClasses.Queries
{
    public record GetStudentClassesQuery(StudentClassSpecParam StudentClassSpecParam, Guid StudentId)
        : IRequest<Pagination<StudentClassResponse>>;
}
