using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SubscriptionPlan
{
    public class PlanInUseException : AppException
    {
        public PlanInUseException() : base(CustomCode.PlanInUse)
        {

        }
    }
}