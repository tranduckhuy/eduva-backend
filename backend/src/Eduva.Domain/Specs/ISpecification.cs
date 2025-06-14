using System.Linq.Expressions;

namespace Eduva.Domain.Specs
{
    public interface ISpecification<TEntity>
    {
        Expression<Func<TEntity, bool>> Criteria { get; }
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? OrderBy { get; }
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? OrderByDescending { get; }
        List<Expression<Func<TEntity, object>>> Includes { get; }
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? Selector { get; }
        int Skip { get; }
        int Take { get; }
    }
}
