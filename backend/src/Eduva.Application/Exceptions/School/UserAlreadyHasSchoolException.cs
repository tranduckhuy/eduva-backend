using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.School
{
    public class UserAlreadyHasSchoolException : AppException
    {
        public UserAlreadyHasSchoolException() : base(CustomCode.UserAlreadyHasSchool) { }
    }
}