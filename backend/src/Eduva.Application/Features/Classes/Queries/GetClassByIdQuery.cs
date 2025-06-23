using Eduva.Application.Features.Classes.Responses;
using MediatR;

namespace Eduva.Application.Features.Classes.Queries
{
    public class GetClassByIdQuery : IRequest<ClassResponse>
    {
        public Guid Id { get; }
        public Guid UserId { get; }

        public GetClassByIdQuery(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
        }
    }
}
