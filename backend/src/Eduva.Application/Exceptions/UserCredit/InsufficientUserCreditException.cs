using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.UserCredit
{
    public class InsufficientUserCreditException : AppException
    {
        public InsufficientUserCreditException(IEnumerable<string>? errors)
            : base(CustomCode.InsufficientUserCredit, errors) { }
    }
}
