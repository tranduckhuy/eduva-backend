using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.SchoolSubscriptions.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.SchoolSubscriptions.Queries
{
    public class GetMySchoolSubscriptionQueryHandler : IRequestHandler<GetMySchoolSubscriptionQuery, MySchoolSubscriptionResponse>
    {
        private readonly ISchoolSubscriptionRepository _schoolSubscriptionRepository;
        private readonly IUserRepository _userRepository;

        public GetMySchoolSubscriptionQueryHandler(IUnitOfWork unitOfWork)
        {
            _userRepository = unitOfWork.GetCustomRepository<IUserRepository>();
            _schoolSubscriptionRepository = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
        }

        public async Task<MySchoolSubscriptionResponse> Handle(GetMySchoolSubscriptionQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId)
                  ?? throw new UserNotExistsException();

            if (user.SchoolId is null)
            {
                throw new SchoolNotFoundException();
            }

            var sub = await _schoolSubscriptionRepository.GetLatestPaidBySchoolIdAsync(user.SchoolId.Value, cancellationToken)
                       ?? throw new SchoolSubscriptionNotFoundException();

            var plan = sub.Plan;

            return new MySchoolSubscriptionResponse
            {
                PlanName = plan.Name,
                Description = plan.Description,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                SubscriptionStatus = sub.SubscriptionStatus,
                BillingCycle = sub.BillingCycle,
                MaxUsers = plan.MaxUsers,
                StorageLimitGB = plan.StorageLimitGB,
            };
        }
    }
}