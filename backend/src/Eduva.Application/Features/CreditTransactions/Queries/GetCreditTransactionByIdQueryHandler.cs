using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.CreditTransactions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.CreditTransactions.Queries
{
    public class GetCreditTransactionByIdQueryHandler : IRequestHandler<GetCreditTransactionByIdQuery, CreditTransactionResponse>
    {
        private readonly ICreditTransactionRepository _repository;

        public GetCreditTransactionByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _repository = unitOfWork.GetCustomRepository<ICreditTransactionRepository>();
        }

        public async Task<CreditTransactionResponse> Handle(GetCreditTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            var creditTransaction = await _repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

            return creditTransaction is null
                ? throw new AppException(CustomCode.CreditTransactionNotFound)
                : AppMapper.Mapper.Map<CreditTransactionResponse>(creditTransaction);
        }
    }
}