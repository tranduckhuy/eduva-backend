using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.SchoolSubscription
{
    public class SchoolSubscriptionAlreadyExistsException : AppException
    {
        public SchoolSubscriptionAlreadyExistsException() : base(CustomCode.SchoolSubscriptionAlreadyExists)
        {
        }
    }
}
