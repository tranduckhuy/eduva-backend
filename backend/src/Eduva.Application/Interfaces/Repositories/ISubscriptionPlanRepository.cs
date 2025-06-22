using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan, int>
    {
        Task<SubscriptionPlan> GetPlanByTransactionIdAsync(Guid transactionId);
    }
}