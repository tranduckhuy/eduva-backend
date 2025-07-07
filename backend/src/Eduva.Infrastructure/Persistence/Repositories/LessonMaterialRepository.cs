using Eduva.Application.Features.LessonMaterials.DTOs;
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

        // SystemAdmin, SchoolAdmin, Content Moderator can view all lesson materials in a folder
        public async Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderAsync(Guid folderId, int schoolId,
            LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default)
        {
            var query = _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .Include(lm => lm.FolderLessonMaterials)
                .Where(lm => lm.FolderLessonMaterials.Any(flm => flm.FolderId == folderId) && lm.SchoolId == schoolId);

            // Apply filters
            query = ApplyLessonMaterialFilters(query, filterOptions);

            return await query.AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderForTeacherAsync(Guid folderId, Guid teacherId, int schoolId, LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default)
        {
            var query = _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .Include(lm => lm.FolderLessonMaterials)
                .ThenInclude(flm => flm.Folder)
                .ThenInclude(f => f.Class)
                .Where(lm => lm.FolderLessonMaterials.Any(flm => flm.FolderId == folderId) && lm.SchoolId == schoolId);


            // Apply filters
            query = ApplyLessonMaterialFilters(query, filterOptions);

            return await query.AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderForStudentAsync(Guid folderId, Guid studentId, int schoolId, LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default)
        {
            var folderClassId = await _context.Folders
                .Where(f => f.Id == folderId)
                .Select(f => f.ClassId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!folderClassId.HasValue)
            {
                return [];
            }

            var isEnrolled = await _studentClassRepository
                .IsStudentEnrolledInClassAsync(studentId, folderClassId.Value);

            if (!isEnrolled)
            {
                return [];
            }

            var query = _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .Include(lm => lm.FolderLessonMaterials)
                .Where(lm => lm.FolderLessonMaterials.Any(flm => flm.FolderId == folderId) &&
                           lm.Status == EntityStatus.Active &&
                           lm.LessonStatus == LessonMaterialStatus.Approved && lm.SchoolId == schoolId);

            // Apply filters (note: for students, Status and LessonStatus filters are ignored to maintain security)
            var studentFilterOptions = filterOptions != null ? new LessonMaterialFilterOptions
            {
                SearchTerm = filterOptions.SearchTerm,
                SortBy = filterOptions.SortBy,
                SortDirection = filterOptions.SortDirection,
                // Status and LessonStatus are fixed for students for security
                Status = null,
                LessonStatus = null
            } : null;

            query = ApplyLessonMaterialFilters(query, studentFilterOptions);

            return await query.AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        private IQueryable<LessonMaterial> ApplyLessonMaterialSorting(IQueryable<LessonMaterial> query, string? sortBy, string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query.OrderBy(lm => lm.CreatedAt);
            }

            var isDescending = !string.IsNullOrWhiteSpace(sortDirection) && sortDirection.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "title" => isDescending ? query.OrderByDescending(lm => lm.Title) : query.OrderBy(lm => lm.Title),
                "createdat" => isDescending ? query.OrderByDescending(lm => lm.CreatedAt) : query.OrderBy(lm => lm.CreatedAt),
                "lastmodifiedat" => isDescending ? query.OrderByDescending(lm => lm.LastModifiedAt) : query.OrderBy(lm => lm.LastModifiedAt),
                "filesize" => isDescending ? query.OrderByDescending(lm => lm.FileSize) : query.OrderBy(lm => lm.FileSize),
                "duration" => isDescending ? query.OrderByDescending(lm => lm.Duration) : query.OrderBy(lm => lm.Duration),
                "lessonstatus" => isDescending ? query.OrderByDescending(lm => lm.LessonStatus) : query.OrderBy(lm => lm.LessonStatus),
                _ => query.OrderBy(lm => lm.CreatedAt)
            };
        }

        private IQueryable<LessonMaterial> ApplyLessonMaterialFilters(IQueryable<LessonMaterial> query, LessonMaterialFilterOptions? filterOptions)
        {
            if (filterOptions == null)
                return query;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filterOptions.SearchTerm))
            {
                query = query.Where(lm => lm.Title.Contains(filterOptions.SearchTerm) ||
                                        (lm.Description != null && lm.Description.Contains(filterOptions.SearchTerm)) ||
                                        lm.SourceUrl.Contains(filterOptions.SearchTerm));
            }

            // Apply LessonStatus filter
            if (filterOptions.LessonStatus.HasValue)
            {
                query = query.Where(lm => lm.LessonStatus == filterOptions.LessonStatus.Value);
            }

            // Apply Status filter
            if (filterOptions.Status.HasValue)
            {
                query = query.Where(lm => lm.Status == filterOptions.Status.Value);
            }

            // Apply sorting
            query = ApplyLessonMaterialSorting(query, filterOptions.SortBy, filterOptions.SortDirection);

            return query;
        }
    }
}