using Eduva.Application.Features.Payments.Responses;
using MediatR;

namespace Eduva.Application.Features.Payments.Queries
{
    public record GetMySchoolSubscriptionQuery(Guid UserId) : IRequest<MySchoolSubscriptionResponse>;

}