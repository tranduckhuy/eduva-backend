using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.Caching.Distributed;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialRepository : GenericRepository<LessonMaterial, int>, ILessonMaterialRepository
    {
        public LessonMaterialRepository(AppDbContext context, IDistributedCache cache) : base(context)
        {
        }
    }
}
