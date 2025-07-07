using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Responses;
using Eduva.Application.Features.Payments.Specifications;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Payments.Queries
{
    public class GetPaymentTransactionsQueryHandler
        : IRequestHandler<GetPaymentTransactionsQuery, Pagination<PaymentResponse>>
    {
        private readonly IPaymentTransactionRepository _repository;

        public GetPaymentTransactionsQueryHandler(IPaymentTransactionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Pagination<PaymentResponse>> Handle(
            GetPaymentTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var spec = new PaymentSpecification(request.Param);

            var result = await _repository.GetWithSpecAsync(spec);

            return AppMapper<AppMappingProfile>.Mapper.Map<Pagination<PaymentResponse>>(result);
        }
    }
}