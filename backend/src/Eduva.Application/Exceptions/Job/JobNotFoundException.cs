using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Job
{
    public class JobNotFoundException : AppException
    {
        public JobNotFoundException() : base(CustomCode.JobNotFound) { }
    }
}
