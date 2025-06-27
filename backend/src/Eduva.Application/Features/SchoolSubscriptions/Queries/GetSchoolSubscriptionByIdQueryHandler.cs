using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetSchoolSubscriptionByIdQueryHandler : IRequestHandler<GetSchoolSubscriptionByIdQuery, SchoolSubscriptionResponse>
    {
        private readonly ISchoolSubscriptionRepository _repository;

        public GetSchoolSubscriptionByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _repository = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
        }

        public async Task<SchoolSubscriptionResponse> Handle(GetSchoolSubscriptionByIdQuery request, CancellationToken cancellationToken)
        {
            var subscription = await _repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);

            return subscription is null
                ? throw new SchoolSubscriptionNotFoundException()
                : AppMapper.Mapper.Map<SchoolSubscriptionResponse>(subscription);
        }
    }
}