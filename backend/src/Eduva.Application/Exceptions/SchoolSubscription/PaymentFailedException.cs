using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SchoolSubscription
{
    public class PaymentFailedException : AppException
    {
        public PaymentFailedException() : base(CustomCode.PaymentFailed)
        {
        }
    }
}