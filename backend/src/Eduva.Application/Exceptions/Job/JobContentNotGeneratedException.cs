using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Job
{
    public class JobContentNotGeneratedException : AppException
    {
        public JobContentNotGeneratedException(IEnumerable<string>? errors)
            : base(CustomCode.JobContentNotGenerated, errors) { }
    }
}
