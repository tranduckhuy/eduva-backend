using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.AICreditPack
{
    public class AICreditPackNotFoundException : AppException
    {
        public AICreditPackNotFoundException() : base(CustomCode.AICreditPackNotFound)
        {
        }
    }
}
