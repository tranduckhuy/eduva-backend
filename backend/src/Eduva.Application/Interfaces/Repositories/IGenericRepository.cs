using Eduva.Application.Common.Models;
using Eduva.Application.Common.Specifications;
using System.Linq.Expressions;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : class
    {
        Task<Pagination<TEntity>> GetWithSpecAsync<TSpec>(TSpec spec) where TSpec : ISpecification<TEntity>;
        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(TKey id);
        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
        Task<bool> ExistsAsync(TKey id);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
        Task<TEntity?> FindAsync(TKey id);
    }
}