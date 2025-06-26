using Eduva.Application.Features.CreditTransactions.Responses;
using MediatR;

namespace Eduva.Application.Features.CreditTransactions.Queries
{
    public class GetCreditTransactionByIdQuery : IRequest<CreditTransactionResponse>
    {
        public Guid Id { get; set; }

        public GetCreditTransactionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}