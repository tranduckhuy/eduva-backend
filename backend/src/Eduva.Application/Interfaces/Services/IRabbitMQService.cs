using Eduva.Application.Features.Jobs.DTOs;

namespace Eduva.Application.Interfaces.Services;

public interface IRabbitMQService
{
    void Dispose();
    Task PublishAsync<T>(T message, TaskType taskType) where T : class;
}
