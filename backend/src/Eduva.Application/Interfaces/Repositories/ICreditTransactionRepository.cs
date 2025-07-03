using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ICreditTransactionRepository : IGenericRepository<UserCreditTransaction, Guid>
    {
        Task<UserCreditTransaction?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    }
}