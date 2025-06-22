using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.AICreditPack
{
    public class AICreditPackAlreadyActiveException : AppException
    {
        public AICreditPackAlreadyActiveException() : base(CustomCode.AICreditPackAlreadyActive)
        {
        }
    }
}