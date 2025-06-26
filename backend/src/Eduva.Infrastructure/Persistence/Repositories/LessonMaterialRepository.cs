using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialRepository : GenericRepository<LessonMaterial, Guid>, ILessonMaterialRepository
    {
        public LessonMaterialRepository(AppDbContext context) : base(context)
        {
        }
    }
}
