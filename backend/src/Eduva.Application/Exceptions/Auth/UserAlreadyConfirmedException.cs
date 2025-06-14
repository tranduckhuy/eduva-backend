using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserAlreadyConfirmedException : AppException
    {
        public UserAlreadyConfirmedException() : base(CustomCode.UserAlreadyConfirmed) { }
    }
}
