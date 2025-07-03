using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ILessonMaterialRepository : IGenericRepository<LessonMaterial, Guid>
    {
        Task<LessonMaterial?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
