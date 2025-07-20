namespace Eduva.Infrastructure.Configurations;

public class RabbitMQConfiguration
{
    public string ConnectionUri { get; set; } = "amqp://guest:guest@localhost:5672/";
    public string QueueName { get; set; } = "eduva_ai_tasks_queue";
    public string ExchangeName { get; set; } = "eduva_exchange";
    public string RoutingKey { get; set; } = "eduva_routing_key";

    public string? DeadLetterQueueName { get; set; } = "eduva_ai_tasks_dlq_queue";
    public string? DeadLetterExchange { get; set; } = "eduva_ai_task_dlq";
    public string? DeadLetterRoutingKey { get; set; } = "eduva_dlq_routing_key";
}
