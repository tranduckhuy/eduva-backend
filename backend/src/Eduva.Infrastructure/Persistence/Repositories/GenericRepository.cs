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
        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public Task<TEntity?> FindAsync(TKey id)
        {
            return GetByIdAsync(id);
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet;

            if (include != null)
            {
                query = include(query);
            }

            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(predicate, cancellationToken);
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

            // Sorting - ensure there's always an OrderBy for consistent pagination
            if (spec.OrderBy != null)
            {
                query = spec.OrderBy(query);
            }
            else
            {
                // Simple default ordering to prevent EF Core warning
                query = query.OrderBy(e => EF.Property<object>(e, "Id"));
            }

            // Projection
            if (spec.Selector != null)
            {
                query = spec.Selector(query);
            }

            var count = await query.CountAsync();


            var data = new List<TEntity>();
            if (spec.Skip == 0 && spec.Take == int.MaxValue)
            {
                data = await query.AsNoTracking().ToListAsync();
                return new Pagination<TEntity>(1, count, count, data);
            }

            data = await query.Skip(spec.Skip).Take(spec.Take).AsNoTracking().ToListAsync();

            var pageNumber = spec.Take > 0 ? (spec.Skip / spec.Take) + 1 : 1;

            return new Pagination<TEntity>(pageNumber, spec.Take, count, data);
        }
    }
}