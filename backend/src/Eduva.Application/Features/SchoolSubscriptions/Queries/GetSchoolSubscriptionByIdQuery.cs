using Eduva.Application.Features.SchoolSubscriptions.Responses;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetSchoolSubscriptionByIdQuery : IRequest<SchoolSubscriptionResponse>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsSystemAdmin { get; set; }

        public GetSchoolSubscriptionByIdQuery(Guid id, Guid userId, bool isSystemAdmin)
        {
            Id = id;
            UserId = userId;
            IsSystemAdmin = isSystemAdmin;
        }
    }
}