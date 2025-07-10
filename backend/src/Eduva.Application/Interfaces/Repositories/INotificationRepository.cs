using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface INotificationRepository : IGenericRepository<Notification, Guid>
    {
        Task<List<Notification>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    }
}