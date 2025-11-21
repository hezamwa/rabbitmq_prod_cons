namespace WH.SharedKernel.RabbitMqMessaging.Producer;

/// <summary>
/// Defines a contract for publishing messages to RabbitMQ.
/// </summary>
public interface IRabbitMqProducer
{
    /// <summary>
    /// Publishes a message to the configured exchange.
    /// </summary>
    /// <param name="message">The message to serialize and send.</param>
    /// <param name="routingKeyOverride">Optional routing key override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<T>(T message, string? routingKeyOverride = null, CancellationToken cancellationToken = default);
}

