using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class ForbiddenException : AppException
    {
        public ForbiddenException(IEnumerable<string>? errors = null) : base(CustomCode.Forbidden, errors)
        {
        }
    }
}
