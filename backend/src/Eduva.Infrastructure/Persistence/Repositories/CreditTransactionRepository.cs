using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class CreditTransactionRepository : GenericRepository<UserCreditTransaction, Guid>, ICreditTransactionRepository
    {
        public CreditTransactionRepository(AppDbContext context) : base(context)
        {

        }
        public async Task<UserCreditTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.UserCreditTransactions
                .Include(x => x.User)
                .Include(x => x.AICreditPack)
                .Include(x => x.PaymentTransaction)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}