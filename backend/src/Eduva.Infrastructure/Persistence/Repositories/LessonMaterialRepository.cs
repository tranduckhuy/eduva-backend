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
    }
}