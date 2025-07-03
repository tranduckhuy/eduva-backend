using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class PlanNotFoundException : AppException
    {
        public PlanNotFoundException() : base(CustomCode.PlanNotFound)
        {
        }
    }
}