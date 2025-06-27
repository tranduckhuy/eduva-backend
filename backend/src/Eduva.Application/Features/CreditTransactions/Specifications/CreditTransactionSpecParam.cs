using Eduva.Application.Common.Specifications;

namespace Eduva.Application.Features.CreditTransactions.Specifications
{
    public class CreditTransactionSpecParam : BaseSpecParam
    {
        public Guid UserId { get; set; }
        public int AICreditPackId { get; set; }
    }
}