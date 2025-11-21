using System.ComponentModel.DataAnnotations;

namespace WH.SharedKernel.RabbitMqMessaging.Options;

/// <summary>
/// Strongly typed configuration for RabbitMQ connectivity and topology.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// The RabbitMQ host name. Defaults to localhost.
    /// </summary>
    [Required]
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// The port used for AMQP connections. Defaults to 5672.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    /// <summary>
    /// The virtual host to connect to. Defaults to '/'.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The username used for authentication. Defaults to guest.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// The password used for authentication. Defaults to guest.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// The exchange name that will be declared and used for publishing.
    /// </summary>
    public string ExchangeName { get; set; } = string.Empty;

    /// <summary>
    /// The exchange type (direct, topic, fanout, etc.). Defaults to direct.
    /// </summary>
    public string ExchangeType { get; set; } = "direct";

    /// <summary>
    /// The queue name that consumers will listen to.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// The default routing key used when publishing.
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether failed messages should be requeued. Defaults to true.
    /// </summary>
    public bool RequeueOnFailure { get; set; } = true;
}

