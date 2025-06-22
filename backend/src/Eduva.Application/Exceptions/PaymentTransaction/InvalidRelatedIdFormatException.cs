using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.PaymentTransaction
{
    public class InvalidRelatedIdFormatException : AppException
    {
        public InvalidRelatedIdFormatException() : base(CustomCode.InvalidRelatedIdFormat)
        {
        }
    }
}
