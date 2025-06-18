using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SchoolSubscription
{
    public class DowngradeNotAllowedException : AppException
    {
        public DowngradeNotAllowedException() : base(CustomCode.DowngradeNotAllowed)
        {
        }
    }
}