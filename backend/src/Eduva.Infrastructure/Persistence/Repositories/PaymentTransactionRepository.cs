using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class PaymentTransactionRepository : GenericRepository<PaymentTransaction, Guid>, IPaymentTransactionRepository
    {
        public PaymentTransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PaymentTransaction?> GetByTransactionCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.TransactionCode == code, cancellationToken);
        }
    }
}
