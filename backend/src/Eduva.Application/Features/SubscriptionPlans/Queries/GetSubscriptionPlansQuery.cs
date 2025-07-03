using Eduva.Application.Common.Models;
using Eduva.Application.Features.SubscriptionPlans.Responses;
using Eduva.Application.Features.SubscriptionPlans.Specifications;
using MediatR;

namespace Eduva.Application.Features.SubscriptionPlans.Queries
{
    public record GetSubscriptionPlansQuery(SubscriptionPlanSpecParam Param) : IRequest<Pagination<SubscriptionPlanResponse>>;
}