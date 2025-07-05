using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class LessonMaterialRepository : GenericRepository<LessonMaterial, Guid>, ILessonMaterialRepository
    {
        private readonly IStudentClassRepository _studentClassRepository;
        public LessonMaterialRepository(AppDbContext context, IStudentClassRepository studentClassRepository) : base(context)
        {
            _studentClassRepository = studentClassRepository;
        }

        public async Task<LessonMaterial?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LessonMaterials
                .Include(lm => lm.FolderLessonMaterials)
                .Include(lm => lm.CreatedByUser)
                .FirstOrDefaultAsync(lm => lm.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<LessonMaterial>> GetAllBySchoolAsync(Guid userId, bool isStudent, int? schoolId = null, Guid? classId = null, Guid? folderId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .AsQueryable();

            if (isStudent)
            {
                if (folderId.HasValue)
                {
                    var folderClassId = await _context.Folders
                        .Where(f => f.Id == folderId)
                        .Select(f => f.ClassId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (folderClassId.HasValue)
                    {
                        var isEnrolled = await _studentClassRepository
                            .IsStudentEnrolledInClassAsync(userId, folderClassId.Value);

                        if (!isEnrolled)
                        {
                            return new List<LessonMaterial>();
                        }
                    }

                    query = query.Where(lm => lm.FolderLessonMaterials.Any(flm => flm.FolderId == folderId));
                }
                else if (classId.HasValue)
                {
                    var isEnrolled = await _studentClassRepository
                        .IsStudentEnrolledInClassAsync(userId, classId.Value);

                    if (!isEnrolled)
                    {
                        return new List<LessonMaterial>();
                    }

                    query = query.Where(lm => lm.FolderLessonMaterials.Any(flm => flm.Folder.ClassId == classId));
                }
                else
                {
                    return new List<LessonMaterial>();
                }

                query = query.Where(lm =>
                    lm.Status == EntityStatus.Active &&
                    lm.LessonStatus == LessonMaterialStatus.Approved);
            }
            else
            {
                query = query.Where(lm =>
                    (!schoolId.HasValue || lm.SchoolId == schoolId) &&
                    (lm.Visibility == LessonMaterialVisibility.School ||
                     (lm.Visibility == LessonMaterialVisibility.Private && lm.CreatedByUserId == userId)));

                if (folderId.HasValue)
                {
                    query = query.Where(lm => lm.FolderLessonMaterials.Any(flm => flm.FolderId == folderId));
                }
                else if (classId.HasValue)
                {
                    query = query.Where(lm => lm.FolderLessonMaterials.Any(flm => flm.Folder.ClassId == classId));
                }
                else
                {
                    return new List<LessonMaterial>();
                }
            }

            return await query.OrderBy(lm => lm.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        public async Task<int> CountApprovedMaterialsInFoldersAsync(List<Guid> folderIds, CancellationToken cancellationToken = default)
        {
            if (folderIds == null || folderIds.Count == 0)
                return 0;

            var count = await _context.FolderLessonMaterials
        .Where(flm => folderIds.Contains(flm.FolderId))
        .Join(_context.LessonMaterials,
            flm => flm.LessonMaterialId,
            lm => lm.Id,
            (flm, lm) => new { FolderId = flm.FolderId, IsApproved = lm.LessonStatus == LessonMaterialStatus.Approved })
        .Where(x => x.IsApproved)
        .CountAsync(cancellationToken);

            return count;
        }

        public async Task<int> CountApprovedMaterialsInFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
        {
            return await CountApprovedMaterialsInFoldersAsync(new List<Guid> { folderId }, cancellationToken);
        }

        public async Task<Dictionary<Guid, int>> GetApprovedMaterialCountsByFolderAsync(List<Guid> folderIds, CancellationToken cancellationToken = default)
        {
            if (folderIds == null || folderIds.Count == 0)
                return new Dictionary<Guid, int>();

            var folderMaterialCounts = await _context.FolderLessonMaterials
                .Where(flm => folderIds.Contains(flm.FolderId))
                .Join(_context.LessonMaterials,
                    flm => flm.LessonMaterialId,
                    lm => lm.Id,
                    (flm, lm) => new { FolderId = flm.FolderId, IsApproved = lm.LessonStatus == LessonMaterialStatus.Approved })
                .Where(x => x.IsApproved)
                .GroupBy(x => x.FolderId)
                .Select(g => new { FolderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.FolderId, x => x.Count, cancellationToken);

            var result = folderIds.ToDictionary(id => id, id => folderMaterialCounts.TryGetValue(id, out var count) ? count : 0);

            return result;
        }

        public async Task<long> GetTotalFileSizeBySchoolAsync(int schoolId, CancellationToken cancellationToken = default)
        {
            return await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId && lm.Status == EntityStatus.Active)
                .SumAsync(lm => (long)lm.FileSize, cancellationToken);
        }
    }
}