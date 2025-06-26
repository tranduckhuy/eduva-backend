using Eduva.Application.Features.SchoolSubscriptions.Responses;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetSchoolSubscriptionByIdQuery : IRequest<SchoolSubscriptionResponse>
    {
        public Guid Id { get; set; }

        public GetSchoolSubscriptionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}