using Eduva.Application.Features.CreditTransactions.Responses;
using MediatR;

namespace Eduva.Application.Features.CreditTransactions.Queries
{
    public class GetCreditTransactionByIdQuery : IRequest<CreditTransactionResponse>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsSystemAdmin { get; set; }

        public GetCreditTransactionByIdQuery(Guid id, Guid userId, bool isSystemAdmin)
        {
            Id = id;
            UserId = userId;
            IsSystemAdmin = isSystemAdmin;
        }
    }
}