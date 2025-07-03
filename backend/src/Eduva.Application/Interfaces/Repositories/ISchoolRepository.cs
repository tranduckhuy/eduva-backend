using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISchoolRepository : IGenericRepository<School, int>
    {
        Task<School?> GetByUserIdAsync(Guid userId);
        Task<(int currentUserCount, int maxUserLimit)> GetUserLimitInfoByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    }
}