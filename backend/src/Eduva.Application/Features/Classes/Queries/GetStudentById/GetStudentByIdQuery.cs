using Eduva.Application.Features.Classes.Responses;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries.GetStudentById
{
    public class GetStudentByIdQuery : IRequest<StudentClassResponse>
    {
        public Guid Id { get; }
        public Guid UserId { get; }
        public GetStudentByIdQuery(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }
}
