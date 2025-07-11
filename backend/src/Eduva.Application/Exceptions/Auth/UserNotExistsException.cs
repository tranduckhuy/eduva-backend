using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserNotExistsException : AppException
    {
        public UserNotExistsException(IEnumerable<string>? errors = null) : base(CustomCode.UserNotExists, errors) { }
    }
}