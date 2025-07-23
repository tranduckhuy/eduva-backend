using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class FolderRepository : GenericRepository<Folder, Guid>, IFolderRepository
    {
        public FolderRepository(AppDbContext context) : base(context)
        {
        }

        // Get the maximum order of folders for a specific user or class
        public async Task<int> GetMaxOrderAsync(Guid? userId, Guid? classId)
        {
            if (userId.HasValue)
            {
                var maxOrder = await _context.Set<Folder>()
                    .Where(f => f.UserId == userId && f.Status == EntityStatus.Active)
                    .MaxAsync(f => (int?)f.Order) ?? 0;

                return maxOrder;
            }
            else if (classId.HasValue)
            {
                var maxOrder = await _context.Set<Folder>()
                    .Where(f => f.ClassId == classId && f.Status == EntityStatus.Active)
                    .MaxAsync(f => (int?)f.Order) ?? 0;

                return maxOrder;
            }

            return 0;
        }

        public async Task<Folder?> GetFolderWithMaterialsAsync(Guid folderId)
        {
            return await _context.Folders
                .Include(f => f.FolderLessonMaterials)
                    .ThenInclude(flm => flm.LessonMaterial)
                .FirstOrDefaultAsync(f => f.Id == folderId);
        }

        public async Task<IEnumerable<Folder>> ListAsync(Expression<Func<Folder, bool>> predicate, CancellationToken cancellationToken)
        {
            return await _context.Folders.Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<List<Folder>> GetFoldersWithLessonMaterialsByClassIdAsync(Guid classId)
        {
            return await _context.Folders
                .Where(f => f.ClassId == classId)
                .Include(f => f.FolderLessonMaterials)
                    .ThenInclude(flm => flm.LessonMaterial)
                .ToListAsync();
        }
    }
}
