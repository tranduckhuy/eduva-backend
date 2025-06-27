using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetByKeyAsync(string key);
        Task<IEnumerable<SystemConfig>> GetAllAsync(); // For admin dashboard only
        Task UpdateAsync(SystemConfig config);
    }
}
