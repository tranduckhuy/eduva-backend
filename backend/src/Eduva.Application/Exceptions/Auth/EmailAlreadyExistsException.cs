using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class EmailAlreadyExistsException : AppException
    {
        public EmailAlreadyExistsException() : base(CustomCode.EmailAlreadyExists) { }
    }
}
