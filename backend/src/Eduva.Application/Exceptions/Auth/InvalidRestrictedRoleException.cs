using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class InvalidRestrictedRoleException : AppException
    {
        public InvalidRestrictedRoleException() : base(CustomCode.InvalidRestrictedRole)
        {

        }
    }
}
