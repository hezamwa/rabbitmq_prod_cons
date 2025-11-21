using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WH.SharedKernel.RabbitMqMessaging.Options;

namespace WH.SharedKernel.RabbitMqMessaging.Connections;

/// <summary>
/// Default implementation that manages a single reusable RabbitMQ connection.
/// </summary>
public sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly IOptionsMonitor<RabbitMqOptions> _optionsMonitor;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly object _syncRoot = new();

    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionFactory(
        IOptionsMonitor<RabbitMqOptions> optionsMonitor,
        ILogger<RabbitMqConnectionFactory>? logger = null)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? NullLogger<RabbitMqConnectionFactory>.Instance;
    }

    /// <inheritdoc />
    public IConnection GetConnection()
    {
        ThrowIfDisposed();

        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_syncRoot)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            DisposeConnection();

            var options = _optionsMonitor.CurrentValue;
            ValidateOptions(options);

            var connectionFactory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                DispatchConsumersAsync = true
            };

            _logger.LogInformation("Opening RabbitMQ connection to {Host}:{Port}/{VHost}", options.HostName, options.Port, options.VirtualHost);
            _connection = connectionFactory.CreateConnection();
            _connection.CallbackException += ConnectionOnCallbackException;
            _connection.ConnectionShutdown += ConnectionOnConnectionShutdown;
            return _connection;
        }
    }

    private void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new InvalidOperationException("RabbitMQ HostName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
        {
            throw new InvalidOperationException("RabbitMQ ExchangeName must be configured.");
        }
    }

    private void ConnectionOnCallbackException(object? sender, CallbackExceptionEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ connection callback exception occurred.");
    }

    private void ConnectionOnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        _logger.LogWarning("RabbitMQ connection shutdown detected. Reply code {ReplyCode}: {Text}", args.ReplyCode, args.ReplyText);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_syncRoot)
        {
            DisposeConnection();
            _disposed = true;
        }
    }

    private void DisposeConnection()
    {
        if (_connection is null)
        {
            return;
        }

        _connection.CallbackException -= ConnectionOnCallbackException;
        _connection.ConnectionShutdown -= ConnectionOnConnectionShutdown;
        _connection.Dispose();
        _connection = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqConnectionFactory));
        }
    }
}

