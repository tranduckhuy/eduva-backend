using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Infrastructure.Services
{
    public class SchoolSubscriptionService : ISchoolSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SchoolSubscriptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Get the current and latest subscription for a school even if it has expired
        public async Task<SchoolSubscription?> GetCurrentSubscriptionAsync(int schoolId)
        {
            var schoolSubscriptionRepository = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();

            return await schoolSubscriptionRepository.GetLatestSubscriptionBySchoolIdAsync(schoolId);
        }

        // Is the current user linked to a school with an active subscription?
        public async Task<(bool isActive, DateTimeOffset endDate)> GetUserSubscriptionStatusAsync(Guid userId)
        {
            var schoolSubscriptionRepository = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();

            // Get the user's school ID
            var user = await userRepository.GetByIdAsync(userId);
            if (user == null || user.SchoolId == null)
            {
                return (false, DateTimeOffset.MinValue);
            }

            // Check if the school has an active subscription
            var subscription = await schoolSubscriptionRepository.GetLatestSubscriptionBySchoolIdAsync(user.SchoolId.Value);

            if (subscription == null)
            {
                return (false, DateTimeOffset.MinValue);
            }

            var isActive = subscription != null && subscription.EndDate > DateTimeOffset.UtcNow;

            return (isActive, subscription?.EndDate ?? DateTimeOffset.MinValue);
        }

        public Task UpdateSubscriptionStatusAsync(int schoolId, SubscriptionStatus status)
        {
            var schoolSubscriptionRepository = _unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            return schoolSubscriptionRepository.UpdateSubscriptionStatusAsync(schoolId, status);
        }
    }
}
