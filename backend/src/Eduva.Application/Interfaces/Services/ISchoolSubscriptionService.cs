using Eduva.Domain.Entities;
using Eduva.Domain.Enums;

namespace Eduva.Application.Interfaces.Services
{
    public interface ISchoolSubscriptionService
    {
        // Get the current and latest subscription for a school even if it has expired
        Task<SchoolSubscription?> GetCurrentSubscriptionAsync(int schoolId);

        // Update the subscription status for a school
        Task UpdateSubscriptionStatusAsync(int schoolId, SubscriptionStatus status);

        // Get the user's subscription status
        Task<(bool isActive, DateTimeOffset endDate)> GetUserSubscriptionStatusAsync(Guid userId);
    }
}
