using Eduva.Domain.Common;
using Eduva.Domain.Interfaces.Repositories;
using Eduva.Domain.Specs;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        protected readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<TEntity> AddAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TEntity?> GetByIdAsync(TKey id)
        {
            throw new NotImplementedException();
        }

        public Task<Pagination<TEntity>> GetWithSpecAsync<TSpec>(TSpec spec) where TSpec : ISpecification<TEntity>
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> RemoveRangeAsync(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
