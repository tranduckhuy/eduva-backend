using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class SchoolSubscriptionRepository : GenericRepository<SchoolSubscription, Guid>, ISchoolSubscriptionRepository
    {
        public SchoolSubscriptionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<SchoolSubscription?> FindByTransactionIdAsync(string transactionId)
        {
            return await _context.SchoolSubscriptions
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId);
        }

        public async Task<SchoolSubscription?> GetActiveSubscriptionBySchoolIdAsync(int schoolId)
        {
            return await _context.SchoolSubscriptions
                .Include(x => x.Plan)
                .Where(x =>
                    x.SchoolId == schoolId &&
                    x.SubscriptionStatus == SubscriptionStatus.Active &&
                    x.PaymentStatus == PaymentStatus.Paid &&
                    x.EndDate > DateTimeOffset.UtcNow
                )
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync();
        }
    }
}