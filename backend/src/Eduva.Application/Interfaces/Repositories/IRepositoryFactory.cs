namespace Eduva.Application.Interfaces.Repositories
{
    public interface IRepositoryFactory
    {
        IGenericRepository<TEntity, TKey> GetGenericRepository<TEntity, TKey>()
            where TEntity : class;

        TRepository GetCustomRepository<TRepository>()
            where TRepository : class;
    }
}
