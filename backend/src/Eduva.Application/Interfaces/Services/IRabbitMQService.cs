namespace Eduva.Application.Interfaces.Services;

public interface IRabbitMQService
{
    void Dispose();
    Task PublishAsync<T>(T message, string? routingKey = null) where T : class;
}
