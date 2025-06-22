using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.PaymentTransaction
{
    public class PaymentTransactionNotFoundException : AppException
    {
        public PaymentTransactionNotFoundException() : base(CustomCode.PaymentTransactionNotFound)
        {

        }
    }
}
