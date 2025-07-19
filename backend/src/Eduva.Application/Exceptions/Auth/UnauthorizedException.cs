using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(IEnumerable<string>? errors = null) : base(CustomCode.Unauthorized, errors)
        {
        }
    }
}
