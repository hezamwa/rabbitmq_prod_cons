using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WH.SharedKernel.RabbitMqMessaging.Connections;
using WH.SharedKernel.RabbitMqMessaging.Consumer;
using WH.SharedKernel.RabbitMqMessaging.Options;
using WH.SharedKernel.RabbitMqMessaging.Producer;

namespace WH.SharedKernel.RabbitMqMessaging.Extensions;

/// <summary>
/// Dependency injection helpers for RabbitMQ messaging services.
/// </summary>
public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Registers RabbitMQ messaging services and configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureOptions">Options delegate.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        Action<RabbitMqOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddOptions<RabbitMqOptions>()
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ExchangeName),
                "RabbitMq:ExchangeName must be provided.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.RoutingKey),
                "RabbitMq:RoutingKey must be provided.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.QueueName),
                "RabbitMq:QueueName must be provided.");

        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqChannelProvider, RabbitMqChannelProvider>();
        services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();
        services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();

        return services;
    }
}

