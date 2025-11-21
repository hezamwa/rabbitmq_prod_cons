using RabbitMQ.Client;

namespace WH.SharedKernel.RabbitMqMessaging.Connections;

/// <summary>
/// Provides access to a reusable RabbitMQ connection.
/// </summary>
public interface IRabbitMqConnectionFactory : IDisposable
{
    /// <summary>
    /// Gets an open RabbitMQ connection, creating it if needed.
    /// </summary>
    IConnection GetConnection();
}

