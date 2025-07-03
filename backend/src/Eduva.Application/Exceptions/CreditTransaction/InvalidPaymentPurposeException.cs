using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.CreditTransaction
{
    public class InvalidPaymentPurposeException : AppException
    {
        public InvalidPaymentPurposeException() : base(CustomCode.InvalidPaymentPurpose)
        {
        }
    }
}
