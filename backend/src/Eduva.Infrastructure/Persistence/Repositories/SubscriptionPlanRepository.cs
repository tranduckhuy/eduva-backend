using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan, int>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(AppDbContext context) : base(context)
        {

        }
        public async Task<SubscriptionPlan> GetPlanByTransactionIdAsync(Guid transactionId)
        {
            var subscription = await _context.SchoolSubscriptions
                .Include(ss => ss.Plan)
                .FirstOrDefaultAsync(ss => ss.PaymentTransactionId == transactionId)
                ?? throw new PlanNotFoundException();

            return subscription.Plan;
        }
    }
}