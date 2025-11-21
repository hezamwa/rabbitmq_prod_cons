using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WH.SharedKernel.RabbitMqMessaging.Consumer;
using WH.SharedKernel.RabbitMqMessaging.Extensions;
using WH.SharedKernel.RabbitMqMessaging.Producer;
using WH.SharedKernel.RabbitMqMessaging.Sample;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddRabbitMqMessaging(options =>
{
    builder.Configuration.GetSection("RabbitMq").Bind(options);
});

using var host = builder.Build();
await host.StartAsync();

await using var scope = host.Services.CreateAsyncScope();
var producer = scope.ServiceProvider.GetRequiredService<IRabbitMqProducer>();
var consumer = scope.ServiceProvider.GetRequiredService<IRabbitMqConsumer>();

using var shutdownCts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownCts.Cancel();
    consumer.StopConsuming();
};

Console.WriteLine("RabbitMQ Messaging Sample");
Console.WriteLine("-------------------------");
Console.WriteLine("A consumer will start automatically. Press ENTER to publish messages.");


Console.ReadLine();

for (var i = 1; i <= 3; i++)
{
    var demoMessage = new DemoMessage
    {
        Id = Guid.NewGuid(),
        Content = $"Hello RabbitMQ #{i}",
        CreatedAt = DateTime.UtcNow
    };

    await producer.PublishAsync(demoMessage, cancellationToken: shutdownCts.Token);
    Console.WriteLine($"Published message #{i}: {demoMessage.Id}");
}

consumer.StartConsuming<DemoMessage>(async (message, _) =>
{
    Console.WriteLine($"[{DateTimeOffset.Now:O}] Received message {message.Id} - {message.Content} (CreatedAt: {message.CreatedAt:O})");
    await Task.CompletedTask;
});


Console.WriteLine("Messages published. Press ENTER to stop the consumer and exit.");
Console.ReadLine();

consumer.StopConsuming();

await host.StopAsync();

Console.WriteLine("Shutdown complete. Goodbye!");
