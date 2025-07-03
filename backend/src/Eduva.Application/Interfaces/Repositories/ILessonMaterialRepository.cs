using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ILessonMaterialRepository : IGenericRepository<LessonMaterial, Guid>
    {
        Task<LessonMaterial?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LessonMaterial>> GetAllBySchoolAsync(Guid userId, bool isStudent, int? schoolId = null, Guid? classId = null, Guid? folderId = null, CancellationToken cancellationToken = default);
    }
}
