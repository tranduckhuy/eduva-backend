using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Services
{
    public interface ISchoolSubscriptionService
    {
        // Get the current and latest subscription for a school even if it has expired
        Task<SchoolSubscription?> GetCurrentSubscriptionAsync(int schoolId);
    }
}
