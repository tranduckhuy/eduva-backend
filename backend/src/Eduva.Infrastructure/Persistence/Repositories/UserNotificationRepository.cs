using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class UserNotificationRepository : GenericRepository<UserNotification, Guid>, IUserNotificationRepository
    {
        public UserNotificationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<UserNotification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.TargetUserId == userId)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserNotification>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.TargetUserId == userId)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserNotification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.TargetUserId == userId && !un.IsRead)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserNotifications
                .CountAsync(un => un.TargetUserId == userId && !un.IsRead, cancellationToken);
        }

        public async Task<int> GetTotalCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserNotifications
                .CountAsync(un => un.TargetUserId == userId, cancellationToken);
        }

        public async Task MarkAsReadAsync(Guid userNotificationId, CancellationToken cancellationToken = default)
        {
            var userNotification = await _context.UserNotifications
                .FirstOrDefaultAsync(un => un.Id == userNotificationId, cancellationToken);

            if (userNotification != null)
            {
                userNotification.IsRead = true;
                _context.UserNotifications.Update(userNotification);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(un => un.TargetUserId == userId && !un.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            if (unreadNotifications.Count != 0)
            {
                _context.UserNotifications.UpdateRange(unreadNotifications);
            }
        }
    }
}