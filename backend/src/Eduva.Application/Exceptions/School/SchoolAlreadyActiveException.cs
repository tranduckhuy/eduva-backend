using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.School
{
    public class SchoolAlreadyActiveException : AppException
    {
        public SchoolAlreadyActiveException() : base(CustomCode.SchoolAlreadyActive)
        {
        }
    }
}