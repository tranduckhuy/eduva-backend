using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class TwoFactorIsAlreadyEnabledException : AppException
    {
        public TwoFactorIsAlreadyEnabledException() : base(CustomCode.TwoFactorIsAlreadyEnabled)
        {
        }
    }
}