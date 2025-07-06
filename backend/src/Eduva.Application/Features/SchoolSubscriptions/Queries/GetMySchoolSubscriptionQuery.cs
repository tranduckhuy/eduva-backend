using Eduva.Application.Features.SchoolSubscriptions.Responses;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public record GetMySchoolSubscriptionQuery(Guid UserId) : IRequest<MySchoolSubscriptionResponse>;

}