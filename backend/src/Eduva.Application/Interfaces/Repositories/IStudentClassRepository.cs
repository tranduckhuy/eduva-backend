using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IStudentClassRepository : IGenericRepository<StudentClass, int>
    {
        Task<List<Classroom>> GetClassesForStudentAsync(Guid studentId);

        Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId);

        Task<StudentClass?> GetStudentClassAsync(Guid studentId, Guid classId);
    }
}
