using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.SchoolSubscriptions.Specifications
{
    public class SchoolSubscriptionSpecParam : BaseSpecParam
    {
        public SubscriptionStatus? SubscriptionStatus { get; set; }
        public BillingCycle? BillingCycle { get; set; }
        public DateFilter DateFilter { get; set; } = DateFilter.All;
    }
}