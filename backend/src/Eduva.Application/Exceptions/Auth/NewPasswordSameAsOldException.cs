using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class NewPasswordSameAsOldException : AppException
    {
        public NewPasswordSameAsOldException() : base(CustomCode.NewPasswordSameAsOld) { }
    }
}