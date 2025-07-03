using Eduva.Application.Common.Mappings;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetShoolSubscriptionQueryHandler
      : IRequestHandler<GetSchoolSubscriptionQuery, Pagination<SchoolSubscriptionResponse>>
    {
        private readonly ISchoolSubscriptionRepository _repository;

        public GetShoolSubscriptionQueryHandler(ISchoolSubscriptionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Pagination<SchoolSubscriptionResponse>> Handle(
            GetSchoolSubscriptionQuery request,
            CancellationToken cancellationToken)
        {
            var spec = new SchoolSubscriptionSpecification(request.Param);

            var result = await _repository.GetWithSpecAsync(spec);

            return AppMapper.Mapper.Map<Pagination<SchoolSubscriptionResponse>>(result);
        }
    }
}