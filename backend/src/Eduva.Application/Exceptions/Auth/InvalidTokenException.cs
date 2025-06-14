using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class InvalidTokenException : AppException
    {
        public InvalidTokenException(IEnumerable<string>? errors = null)
            : base(CustomCode.InvalidToken, errors) { }

    }
}
