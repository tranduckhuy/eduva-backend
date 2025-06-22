using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Auth
{
    public class UserNotPartOfSchoolException : AppException
    {
        public UserNotPartOfSchoolException() : base(CustomCode.UserNotPartOfSchool) { }
    }
}