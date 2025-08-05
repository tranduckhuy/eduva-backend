using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using System.Text;

namespace Eduva.Infrastructure.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQConfiguration _configuration;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQService(RabbitMQConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_configuration.ConnectionUri)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange and queue
        _channel.ExchangeDeclare(
            exchange: _configuration.ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Declare DLX and DLQ if configured
        Dictionary<string, object>? queueArguments = null;

        if (!string.IsNullOrEmpty(_configuration.DeadLetterExchange) &&
            !string.IsNullOrEmpty(_configuration.DeadLetterQueueName) &&
            !string.IsNullOrEmpty(_configuration.DeadLetterRoutingKey))
        {
            // Declare Dead Letter Exchange
            _channel.ExchangeDeclare(
                exchange: _configuration.DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Setup DLQ queue
            _channel.QueueDeclare(
                queue: _configuration.DeadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind DLQ to DLX with routing key
            _channel.QueueBind(
                queue: _configuration.DeadLetterQueueName,
                exchange: _configuration.DeadLetterExchange,
                routingKey: _configuration.DeadLetterRoutingKey);

            // Set queue arguments to point to DLX
            queueArguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _configuration.DeadLetterExchange },
                { "x-dead-letter-routing-key", _configuration.DeadLetterRoutingKey }
            };
        }

        // Declare main queue with optional DLX arguments
        foreach (var kvp in _configuration.QueueNames)
        {
            var taskType = kvp.Key;
            var queueName = kvp.Value;
            var routingKey = _configuration.RoutingKeys[taskType];

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);

            _channel.QueueBind(
                queue: queueName,
                exchange: _configuration.ExchangeName,
                routingKey: routingKey);
        }

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ connection established successfully");
    }

    async Task IRabbitMQService.PublishAsync<T>(T message, TaskType taskType)
    {
        if (!_configuration.RoutingKeys.TryGetValue(taskType, out var routingKey))
        {
            _logger.LogError("No routing key configured for task type: {TaskType}", taskType);
            return;
        }

        var jsonMessage = JsonConvert.SerializeObject(message, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var body = Encoding.UTF8.GetBytes(jsonMessage);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _configuration.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published message to RabbitMQ with routing key: {RoutingKey}", routingKey);
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
