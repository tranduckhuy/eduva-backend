using Eduva.Application.Contracts.Hubs;
using Eduva.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services;

public class JobNotificationService : IJobNotificationService
{
    private readonly INotificationHub _notificationHub;
    private readonly ILogger<JobNotificationService> _logger;

    public JobNotificationService(
        INotificationHub notificationHub,
        ILogger<JobNotificationService> logger)
    {
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationHub.SendNotificationToUserAsync(userId.ToString(), eventName, data);
            _logger.LogInformation("Notification sent to user {UserId}: {EventName}", userId, eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}: {EventName}", userId, eventName);
        }
    }
}
