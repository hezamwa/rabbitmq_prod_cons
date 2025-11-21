using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WH.SharedKernel.RabbitMqMessaging.Connections;
using WH.SharedKernel.RabbitMqMessaging.Options;

namespace WH.SharedKernel.RabbitMqMessaging.Consumer;

/// <summary>
/// Default consumer implementation that uses <see cref="AsyncEventingBasicConsumer"/>.
/// </summary>
public sealed class RabbitMqConsumer : IRabbitMqConsumer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqChannelProvider _channelProvider;
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly object _syncRoot = new();

    private IModel? _consumerChannel;
    private CancellationTokenSource? _cts;
    private string? _consumerTag;

    public RabbitMqConsumer(
        IRabbitMqChannelProvider channelProvider,
        IOptionsMonitor<RabbitMqOptions> optionsMonitor,
        ILogger<RabbitMqConsumer>? logger = null)
    {
        _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? NullLogger<RabbitMqConsumer>.Instance;
    }

    /// <inheritdoc />
    public void StartConsuming<T>(Func<T, CancellationToken, Task> handler, string? queueNameOverride = null)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_syncRoot)
        {
            if (_consumerChannel is not null)
            {
                throw new InvalidOperationException("Consumer already started.");
            }

            var options = _optionsMonitor.CurrentValue;
            var queueName = queueNameOverride ?? options.QueueName;

            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new InvalidOperationException("QueueName must be configured before starting the consumer.");
            }

            _cts = new CancellationTokenSource();
            _consumerChannel = _channelProvider.CreateChannel();

            DeclareInfrastructure(_consumerChannel, options, queueName);
            _consumerChannel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.Received += async (_, eventArgs) =>
            {
                await HandleMessageAsync(eventArgs, handler, queueName, _cts.Token).ConfigureAwait(false);
            };

            _consumerTag = _consumerChannel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming queue {QueueName}", queueName);
        }
    }

    private async Task HandleMessageAsync<T>(
        BasicDeliverEventArgs eventArgs,
        Func<T, CancellationToken, Task> handler,
        string queueName,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = JsonSerializer.Deserialize<T>(eventArgs.Body.Span, SerializerOptions);
            if (message is null)
            {
                throw new InvalidOperationException("Deserialized message was null.");
            }

            await handler(message, cancellationToken).ConfigureAwait(false);
            _consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cancellation requested while processing messages for queue {QueueName}", queueName);
            _consumerChannel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            var requeue = _optionsMonitor.CurrentValue.RequeueOnFailure;
            _logger.LogError(ex, "Error processing RabbitMQ message from queue {QueueName}. Requeue={Requeue}", queueName, requeue);
            _consumerChannel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: requeue);
        }
    }

    /// <inheritdoc />
    public void StopConsuming()
    {
        lock (_syncRoot)
        {
            if (_consumerChannel is null)
            {
                return;
            }

            _cts?.Cancel();

            try
            {
                if (!string.IsNullOrWhiteSpace(_consumerTag))
                {
                    _consumerChannel.BasicCancel(_consumerTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cancel RabbitMQ consumer cleanly.");
            }

            try
            {
                _consumerChannel.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close RabbitMQ consumer channel.");
            }
            finally
            {
                _consumerChannel.Dispose();
                _consumerChannel = null;
            }

            _cts?.Dispose();
            _cts = null;
            _consumerTag = null;

            _logger.LogInformation("Stopped RabbitMQ consumer.");
        }
    }

    private static void DeclareInfrastructure(IModel channel, RabbitMqOptions options, string queueName)
    {
        channel.ExchangeDeclare(
            exchange: options.ExchangeName,
            type: options.ExchangeType,
            durable: true,
            autoDelete: false,
            arguments: null);

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: queueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey);
    }
}

