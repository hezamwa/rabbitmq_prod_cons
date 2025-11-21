namespace WH.SharedKernel.RabbitMqMessaging.Consumer;

/// <summary>
/// Defines a contract for consuming messages from RabbitMQ.
/// </summary>
public interface IRabbitMqConsumer
{
    /// <summary>
    /// Starts consuming messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="handler">Delegate invoked for each message.</param>
    /// <param name="queueNameOverride">Optional queue override.</param>
    void StartConsuming<T>(Func<T, CancellationToken, Task> handler, string? queueNameOverride = null);

    /// <summary>
    /// Stops the consumer and releases resources.
    /// </summary>
    void StopConsuming();
}

