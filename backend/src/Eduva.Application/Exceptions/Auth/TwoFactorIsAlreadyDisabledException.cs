using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class TwoFactorIsAlreadyDisabledException : AppException
    {
        public TwoFactorIsAlreadyDisabledException() : base(CustomCode.TwoFactorIsAlreadyDisabled)
        {
        }
    }
}