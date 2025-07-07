using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetSchoolSubscriptionByIdQueryHandler : IRequestHandler<GetSchoolSubscriptionByIdQuery, SchoolSubscriptionResponse>
    {
        private readonly ISchoolSubscriptionRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public GetSchoolSubscriptionByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _repository = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            _unitOfWork = unitOfWork;
        }

        public async Task<SchoolSubscriptionResponse> Handle(GetSchoolSubscriptionByIdQuery request, CancellationToken cancellationToken)
        {
            var subscription = await _repository.GetByIdWithDetailsAsync(request.Id, cancellationToken) ?? throw new SchoolSubscriptionNotFoundException();

            if (!request.IsSystemAdmin)
            {
                var userRepo = _unitOfWork.GetRepository<ApplicationUser, Guid>();
                var user = await userRepo.GetByIdAsync(request.UserId);

                if (user?.SchoolId != subscription.SchoolId &&
                    subscription.PaymentTransaction?.UserId != request.UserId)
                {
                    throw new SchoolSubscriptionNotFoundException();
                }
            }

            return AppMapper<AppMappingProfile>.Mapper.Map<SchoolSubscriptionResponse>(subscription);
        }
    }
}