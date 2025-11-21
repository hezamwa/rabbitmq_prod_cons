using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace WH.SharedKernel.RabbitMqMessaging.Connections;

/// <summary>
/// Default channel provider that reuses a shared channel and can create dedicated ones.
/// </summary>
public sealed class RabbitMqChannelProvider : IRabbitMqChannelProvider
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqChannelProvider> _logger;
    private readonly object _syncRoot = new();

    private IModel? _sharedChannel;
    private bool _disposed;

    public RabbitMqChannelProvider(
        IRabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqChannelProvider>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? NullLogger<RabbitMqChannelProvider>.Instance;
    }

    /// <inheritdoc />
    public IModel GetOrCreateChannel()
    {
        ThrowIfDisposed();

        if (_sharedChannel is { IsOpen: true })
        {
            return _sharedChannel;
        }

        lock (_syncRoot)
        {
            if (_sharedChannel is { IsOpen: true })
            {
                return _sharedChannel;
            }

            DisposeSharedChannel();
            _sharedChannel = CreateChannelInternal();
            return _sharedChannel;
        }
    }

    /// <inheritdoc />
    public IModel CreateChannel()
    {
        ThrowIfDisposed();
        return CreateChannelInternal();
    }

    private IModel CreateChannelInternal()
    {
        var connection = _connectionFactory.GetConnection();
        var channel = connection.CreateModel();
        _logger.LogDebug("Created RabbitMQ channel with channel number {ChannelNumber}", channel.ChannelNumber);
        return channel;
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
            DisposeSharedChannel();
            _disposed = true;
        }
    }

    private void DisposeSharedChannel()
    {
        if (_sharedChannel is null)
        {
            return;
        }

        try
        {
            _sharedChannel.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ shared channel.");
        }
        finally
        {
            _sharedChannel.Dispose();
            _sharedChannel = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqChannelProvider));
        }
    }
}

