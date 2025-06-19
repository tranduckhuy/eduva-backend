using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.School
{
    public class SchoolNotFoundException : AppException
    {
        public SchoolNotFoundException() : base(CustomCode.SchoolNotFound) { }
    }
}