using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserNotConfirmedException : AppException
    {
        public UserNotConfirmedException() : base(CustomCode.UserNotConfirmed) { }
    }
}