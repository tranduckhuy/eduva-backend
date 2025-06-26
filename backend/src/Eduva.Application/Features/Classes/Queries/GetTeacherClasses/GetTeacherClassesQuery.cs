using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetTeacherClasses
{
    public record GetTeacherClassesQuery(ClassSpecParam ClassSpecParam, Guid TeacherId) 
        : IRequest<Pagination<ClassResponse>>;
}
