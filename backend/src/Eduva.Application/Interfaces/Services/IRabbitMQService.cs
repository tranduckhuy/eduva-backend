namespace Eduva.Application.Interfaces.Services;

public interface IRabbitMQService
{
    Task PublishAsync<T>(T message, string? routingKey = null) where T : class;
}
