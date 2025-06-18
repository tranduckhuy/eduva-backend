using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SchoolSubscription
{
    public class SchoolSubscriptionNotFoundException : AppException
    {
        public SchoolSubscriptionNotFoundException() : base(CustomCode.SchoolSubscriptionNotFound)
        {
        }
    }
}