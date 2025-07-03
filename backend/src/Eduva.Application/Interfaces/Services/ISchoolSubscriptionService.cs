using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Services
{
    public interface ISchoolSubscriptionService
    {
        // Get the current and latest subscription for a school even if it has expired
        Task<SchoolSubscription?> GetCurrentSubscriptionAsync(int schoolId);

        // Get the user's subscription status
        Task<(bool isActive, DateTimeOffset endDate)> GetUserSubscriptionStatusAsync(Guid userId);
    }
}
