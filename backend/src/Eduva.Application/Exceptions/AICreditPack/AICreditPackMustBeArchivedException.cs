using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.AICreditPack
{
    public class AICreditPackMustBeArchivedException : AppException
    {
        public AICreditPackMustBeArchivedException() : base(CustomCode.AICreditPackMustBeArchived)
        {
        }
    }
}