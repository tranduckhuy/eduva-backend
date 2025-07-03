using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.AICreditPack
{
    public class AICreditPackAlreadyArchivedException : AppException
    {
        public AICreditPackAlreadyArchivedException() : base(CustomCode.AICreditPackAlreadyArchived)
        {

        }
    }
}
