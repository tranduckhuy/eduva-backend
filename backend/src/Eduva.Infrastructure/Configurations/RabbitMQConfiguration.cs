using Eduva.Application.Features.Jobs.DTOs;

namespace Eduva.Infrastructure.Configurations;

public class RabbitMQConfiguration
{
    public string ConnectionUri { get; set; } = "amqp://guest:guest@localhost:5679";
    public string ExchangeName { get; set; } = "eduva_exchange";

    public Dictionary<TaskType, string> QueueNames { get; set; } = null!;
    public Dictionary<TaskType, string> RoutingKeys { get; set; } = null!;

    public string? DeadLetterQueueName { get; set; } = "eduva.dlq";
    public string? DeadLetterExchange { get; set; } = "eduva.dlq.exchange";
    public string? DeadLetterRoutingKey { get; set; } = "eduva.dlq.routing_key";
}
