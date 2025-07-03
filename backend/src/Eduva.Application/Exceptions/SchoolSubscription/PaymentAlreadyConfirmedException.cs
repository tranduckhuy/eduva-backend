using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SchoolSubscription
{
    public class PaymentAlreadyConfirmedException : AppException
    {
        public PaymentAlreadyConfirmedException() : base(CustomCode.PaymentAlreadyConfirmed)
        {
        }
    }
}