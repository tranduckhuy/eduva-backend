using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IUserNotificationRepository : IGenericRepository<UserNotification, Guid>
    {
        Task<List<UserNotification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<UserNotification>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetTotalCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid userNotificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}