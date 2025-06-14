using Eduva.Domain.Entities;
using Eduva.Domain.Interfaces.Repositories;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialRepository : GenericRepository<LessonMaterial, int>, ILessonMaterialRepository
    {
        public LessonMaterialRepository(AppDbContext context) : base(context)
        {
        }
    }
}
