using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IClassroomRepository : IGenericRepository<Classroom, Guid>
    {
        Task<Classroom?> FindByClassCodeAsync(string classCode);
    }
}
