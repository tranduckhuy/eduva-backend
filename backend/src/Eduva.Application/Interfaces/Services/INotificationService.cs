using Eduva.Domain.Entities;

namespace Eduva.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(string type, string payload, CancellationToken cancellationToken = default);
        Task CreateUserNotificationsAsync(Guid notificationId, List<Guid> targetUserIds, CancellationToken cancellationToken = default);
        Task<List<UserNotification>> GetUserNotificationsAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
        Task<List<UserNotification>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> GetTotalCountAsync(Guid userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(Guid userNotificationId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetUsersInLessonAsync(Guid lessonMaterialId, CancellationToken cancellationToken = default);
    }
}