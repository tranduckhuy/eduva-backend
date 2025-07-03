using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class SubscriptionPlanMustBeArchivedException : AppException
    {
        public SubscriptionPlanMustBeArchivedException() : base(CustomCode.SubscriptionPlanMustBeArchived)
        {

        }
    }
}
