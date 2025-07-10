using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories;

public interface IJobRepository : IGenericRepository<Job, Guid>
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetExpiredJobsAsync(TimeSpan expiredAfter, CancellationToken cancellationToken = default);
}
