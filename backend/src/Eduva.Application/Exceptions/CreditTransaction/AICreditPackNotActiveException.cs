using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.CreditTransaction
{
    public class AICreditPackNotActiveException : AppException
    {
        public AICreditPackNotActiveException() : base(CustomCode.AICreditPackNotActive)
        {
        }
    }
}