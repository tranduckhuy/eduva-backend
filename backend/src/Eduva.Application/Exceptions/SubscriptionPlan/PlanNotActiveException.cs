using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class PlanNotActiveException : AppException
    {
        public PlanNotActiveException() : base(CustomCode.PlanNotActive)
        {
        }
    }
}