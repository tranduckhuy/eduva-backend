using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.School
{
    public class SchoolAlreadyArchivedException : AppException
    {
        public SchoolAlreadyArchivedException() : base(CustomCode.SchoolAlreadyArchived)
        {
        }
    }
}
