using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserNotExistsException : AppException
    {
        public UserNotExistsException() : base(CustomCode.UserNotExists) { }
    }
}