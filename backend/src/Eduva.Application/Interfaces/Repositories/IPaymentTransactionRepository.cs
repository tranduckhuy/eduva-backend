using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IPaymentTransactionRepository : IGenericRepository<PaymentTransaction, Guid>
    {
        Task<PaymentTransaction?> GetByTransactionCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}
