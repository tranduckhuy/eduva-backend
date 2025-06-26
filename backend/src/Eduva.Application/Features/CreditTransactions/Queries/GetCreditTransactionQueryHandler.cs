using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Features.CreditTransactions.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.CreditTransactions.Queries
{
    public class GetCreditTransactionQueryHandler : IRequestHandler<GetCreditTransactionQuery, Pagination<CreditTransactionResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCreditTransactionQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Pagination<CreditTransactionResponse>> Handle(GetCreditTransactionQuery request, CancellationToken cancellationToken)
        {
            var spec = new CreditTransactionSpecification(request.Param);

            var result = await _unitOfWork
                .GetRepository<UserCreditTransaction, Unit>()
                .GetWithSpecAsync(spec);

            return AppMapper.Mapper.Map<Pagination<CreditTransactionResponse>>(result);
        }
    }
}
