using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WH.SharedKernel.RabbitMqMessaging.Connections;
using WH.SharedKernel.RabbitMqMessaging.Options;

namespace WH.SharedKernel.RabbitMqMessaging.Producer;

/// <summary>
/// Default producer implementation that publishes JSON messages.
/// </summary>
public sealed class RabbitMqProducer : IRabbitMqProducer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly object _publishLock = new();

    public RabbitMqProducer(
        IRabbitMqChannelProvider channelProvider,
        IOptionsMonitor<RabbitMqOptions> optionsMonitor,
        ILogger<RabbitMqProducer>? logger = null)
    {
        _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? NullLogger<RabbitMqProducer>.Instance;
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, string? routingKeyOverride = null, CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var options = _optionsMonitor.CurrentValue;
        ValidateOptions(options);

        var routingKey = routingKeyOverride ?? options.RoutingKey;
        if (string.IsNullOrWhiteSpace(routingKey))
        {
            throw new InvalidOperationException("Routing key must be provided.");
        }

        var channel = _channelProvider.GetOrCreateChannel();
        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);

        lock (_publishLock)
        {
            EnsureExchangeAndQueue(channel, options);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(
                exchange: options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published message of type {MessageType} to exchange {Exchange} with routing key {RoutingKey}", typeof(T).Name, options.ExchangeName, routingKey);
        }

        return Task.CompletedTask;
    }

    private static void EnsureExchangeAndQueue(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(
            exchange: options.ExchangeName,
            type: options.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null);

        if (!string.IsNullOrWhiteSpace(options.QueueName))
        {
            channel.QueueDeclare(
                queue: options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(
                queue: options.QueueName,
                exchange: options.ExchangeName,
                routingKey: options.RoutingKey);
        }
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ExchangeName))
        {
            throw new InvalidOperationException("ExchangeName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeType))
        {
            throw new InvalidOperationException("ExchangeType must be configured.");
        }
    }
}

