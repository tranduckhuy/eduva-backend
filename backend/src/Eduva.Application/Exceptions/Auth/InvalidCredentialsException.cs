using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class InvalidCredentialsException : AppException
    {
        public InvalidCredentialsException() : base(CustomCode.InvalidCredentials) { }
    }
}