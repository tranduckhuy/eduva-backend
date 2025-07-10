using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Eduva.API.Hubs;

[Authorize]
public class JobStatusHub : Hub
{
    private readonly ILogger<JobStatusHub> _logger;

    public JobStatusHub(ILogger<JobStatusHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} connected to JobStatusHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from JobStatusHub", userId);
        await base.OnDisconnectedAsync(exception);
    }
}
