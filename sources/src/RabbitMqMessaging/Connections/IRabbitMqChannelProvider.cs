using RabbitMQ.Client;

namespace WH.SharedKernel.RabbitMqMessaging.Connections;

/// <summary>
/// Provides RabbitMQ channels for publishing and consuming.
/// </summary>
public interface IRabbitMqChannelProvider : IDisposable
{
    /// <summary>
    /// Gets a shared channel instance, lazily creating it if necessary.
    /// </summary>
    IModel GetOrCreateChannel();

    /// <summary>
    /// Creates a dedicated channel that the caller owns and must dispose.
    /// </summary>
    IModel CreateChannel();
}

