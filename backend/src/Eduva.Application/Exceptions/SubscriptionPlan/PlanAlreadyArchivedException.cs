using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class PlanAlreadyArchivedException : AppException
    {
        public PlanAlreadyArchivedException() : base(CustomCode.PlanAlreadyArchived)
        {
        }
    }
}
