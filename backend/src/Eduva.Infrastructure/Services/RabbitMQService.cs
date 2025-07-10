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

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange and queue
            _channel.ExchangeDeclare(
                exchange: _configuration.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Declare dead letter exchange if configured
            if (!string.IsNullOrEmpty(_configuration.DeadLetterExchange))
            {
                _channel.ExchangeDeclare(
                    exchange: _configuration.DeadLetterExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false);
            }

            // Create queue arguments for dead letter exchange if configured
            Dictionary<string, object>? arguments = null;
            if (!string.IsNullOrEmpty(_configuration.DeadLetterExchange))
            {
                arguments = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", _configuration.DeadLetterExchange }
                };
            }

            _channel.QueueDeclare(
                queue: _configuration.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _channel.QueueBind(
                queue: _configuration.QueueName,
                exchange: _configuration.ExchangeName,
                routingKey: _configuration.RoutingKey);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, string? routingKey = null) where T : class
    {
        try
        {
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
                routingKey: routingKey ?? _configuration.RoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message published to RabbitMQ with routing key: {RoutingKey}", routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ with routing key: {RoutingKey}", routingKey);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
