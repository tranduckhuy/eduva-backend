using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class PlanAlreadyActiveException : AppException
    {
        public PlanAlreadyActiveException() : base(CustomCode.PlanAlreadyActive)
        {
        }
    }
}