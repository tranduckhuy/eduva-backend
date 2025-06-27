using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass
{
    public record GetAllStudentsInClassQuery : IRequest<Pagination<StudentClassResponse>>
    {
        public Guid ClassId { get; set; }
        public StudentClassSpecParam StudentClassSpecParam { get; set; }
        public Guid RequesterId { get; set; }

        public GetAllStudentsInClassQuery(Guid classId, StudentClassSpecParam studentClassSpecParam, Guid requesterId)
        {
            ClassId = classId;
            StudentClassSpecParam = studentClassSpecParam;
            RequesterId = requesterId;
        }
    }
}
