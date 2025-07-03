using Eduva.Application.Interfaces.Repositories;

namespace Eduva.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class;
        TRepository GetCustomRepository<TRepository>() where TRepository : class;

        Task<int> CommitAsync();
    }
}