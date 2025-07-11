namespace Eduva.Application.Interfaces.Services;

public interface IJobNotificationService
{
    /// <summary>
    /// Gửi thông báo job cho user
    /// </summary>
    Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken cancellationToken = default);
}
