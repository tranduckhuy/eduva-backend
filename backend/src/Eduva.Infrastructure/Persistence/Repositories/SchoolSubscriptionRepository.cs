using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
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
    }
}