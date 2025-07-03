using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserAccountLockedException : AppException
    {
        public UserAccountLockedException() : base(CustomCode.UserAccountLocked) { }
    }
}