using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialRepository : GenericRepository<LessonMaterial, Guid>, ILessonMaterialRepository
    {
        public LessonMaterialRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<LessonMaterial?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LessonMaterials
                .Include(lm => lm.FolderLessonMaterials)
                .Include(lm => lm.CreatedByUser)
                .FirstOrDefaultAsync(lm => lm.Id == id, cancellationToken);
        }
    }
}
