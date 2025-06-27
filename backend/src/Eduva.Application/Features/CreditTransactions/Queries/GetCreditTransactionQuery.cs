using Eduva.Application.Common.Models;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Features.CreditTransactions.Specifications;
using MediatR;

namespace Eduva.Application.Features.CreditTransactions.Queries
{
    public record GetCreditTransactionQuery(CreditTransactionSpecParam Param) : IRequest<Pagination<CreditTransactionResponse>>;
}