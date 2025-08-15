using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ILessonMaterialRepository : IGenericRepository<LessonMaterial, Guid>
    {
        Task<LessonMaterial?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LessonMaterial>> GetAllBySchoolAsync(Guid userId, bool isStudent, int? schoolId = null, Guid? classId = null, Guid? folderId = null, CancellationToken cancellationToken = default);

        Task<int> CountApprovedMaterialsInFoldersAsync(List<Guid> folderIds, CancellationToken cancellationToken = default);

        Task<int> CountApprovedMaterialsInFolderAsync(Guid folderId, CancellationToken cancellationToken = default);

        Task<Dictionary<Guid, int>> GetApprovedMaterialCountsByFolderAsync(List<Guid> folderIds, CancellationToken cancellationToken = default);

        Task<long> GetTotalFileSizeBySchoolAsync(int schoolId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderAsync(Guid folderId, int schoolId,
            LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderForTeacherAsync(Guid folderId, Guid teacherId,
            int schoolId, LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LessonMaterial>> GetLessonMaterialsByFolderForStudentAsync(Guid folderId, Guid studentId,
            int schoolId, LessonMaterialFilterOptions? filterOptions = null, CancellationToken cancellationToken = default);

        Task<List<LessonMaterial>> GetLessonMaterialsBySchoolOrderedByFileSizeAsync(int schoolId, CancellationToken cancellationToken = default);

        Task<List<LessonMaterial>> GetDeletedMaterialsOlderThan30DaysAsync(CancellationToken cancellationToken = default);
    }
}
