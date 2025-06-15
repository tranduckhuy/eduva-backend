using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class OtpInvalidOrExpireException : AppException
    {
        public OtpInvalidOrExpireException() : base(CustomCode.OtpInvalidOrExpired) { }
    }
}