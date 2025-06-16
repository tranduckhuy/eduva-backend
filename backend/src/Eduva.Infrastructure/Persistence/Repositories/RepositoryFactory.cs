using Eduva.Application.Interfaces.Repositories;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.DependencyInjection;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public RepositoryFactory(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IGenericRepository<TEntity, TKey> GetGenericRepository<TEntity, TKey>()
            where TEntity : class
        {
            return new GenericRepository<TEntity, TKey>(_context);
        }

        public TRepository GetCustomRepository<TRepository>()
            where TRepository : class
        {
            return _serviceProvider.GetService<TRepository>()
                ?? throw new InvalidOperationException($"Repository {typeof(TRepository).Name} not registered");
        }
    }
}
