using Eduva.Domain.Common;
using Eduva.Domain.Specs;

namespace Eduva.Domain.Interfaces.Repositories
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        Task<Pagination<TEntity>> GetWithSpecAsync<TSpec>(TSpec spec) where TSpec : ISpecification<TEntity>;
        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> AddRangeAsync(IEnumerable<TEntity> entities);
        Task UpdateAsync(TEntity entity);
        Task RemoveAsync(TEntity entity);
        Task<TEntity> RemoveRangeAsync(IEnumerable<TEntity> entities);
    }
}
