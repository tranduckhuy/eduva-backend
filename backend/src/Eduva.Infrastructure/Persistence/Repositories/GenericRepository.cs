using Eduva.Application.Common.Models;
using Eduva.Application.Common.Specifications;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task<bool> ExistsAsync(TKey id)
        {
            return await GetByIdAsync(id) != null;
        }

        public Task<TEntity?> FindAsync(TKey id)
        {
            return GetByIdAsync(id);
        }

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<Pagination<TEntity>> GetWithSpecAsync<TSpec>(TSpec spec) where TSpec : ISpecification<TEntity>
        {
            var query = _context.Set<TEntity>().AsQueryable();

            // Filter
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Includes
            foreach (var includeExpression in spec.Includes)
            {
                query = query.Include(includeExpression);
            }

            // Sorting
            if (spec.OrderBy != null)
            {
                query = spec.OrderBy(query);
            }

            // Projection
            if (spec.Selector != null)
            {
                query = spec.Selector(query);
            }

            var count = await query.CountAsync();

            var data = await query.Skip(spec.Skip).Take(spec.Take).AsNoTracking().ToListAsync();

            var pageNumber = spec.Take > 0 ? (spec.Skip / spec.Take) + 1 : 1;

            return new Pagination<TEntity>(pageNumber, spec.Take, count, data);
        }
    }
}