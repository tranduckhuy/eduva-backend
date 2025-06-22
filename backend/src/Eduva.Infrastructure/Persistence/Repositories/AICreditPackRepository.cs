using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    internal class AICreditPackRepository : GenericRepository<AICreditPack, int>, IAICreditPackRepository
    {
        public AICreditPackRepository(AppDbContext context) : base(context)
        {

        }
    }
}