You are an expert C#/.NET developer.

Goal
======
Create a COMPLETE .NET solution that provides a reusable RabbitMQ PRODUCER and CONSUMER implementation as a DLL (class library), with a CONFIGURABLE RabbitMQ address (host/port/credentials/etc.). The goal is to reference this DLL from other applications (e.g., Web API, worker service, console app) and easily send/receive messages.

High-Level Requirements
========================
1. Use modern .NET (prefer .NET 8; if not available, use the latest LTS).
2. Language: C#.
3. Use the official RabbitMQ .NET client:
   - Package: RabbitMQ.Client
4. Produce a CLASS LIBRARY (DLL) project that contains all messaging logic.
5. RabbitMQ connection details MUST be configurable (no hard-coded hostnames).
6. Include a minimal example console app (or two small console apps) that demonstrates:
   - Sending a message with the producer
   - Receiving and processing a message with the consumer

Solution Structure
==================
Create a solution like this (you can adjust names if needed but keep roles clear):

- Solution: RabbitMqMessagingSolution
  - Project 1 (Class Library): RabbitMqMessaging
  - Project 2 (Console App – sample producer/consumer usage): RabbitMqMessaging.Sample

RabbitMqMessaging (Class Library) – Requirements
================================================
This project should compile to a DLL that can be referenced by other .NET apps.

1. Configuration
   - Create a strongly-typed options class, e.g.:

     public class RabbitMqOptions
     {
         public string HostName { get; set; }
         public int Port { get; set; }         // default 5672
         public string VirtualHost { get; set; } // default "/"
         public string UserName { get; set; }  // default "guest"
         public string Password { get; set; }  // default "guest"
         public string ExchangeName { get; set; }
         public string ExchangeType { get; set; } // e.g., "topic" or "direct"
         public string QueueName { get; set; }
         public string RoutingKey { get; set; }
     }

   - The DLL should NOT depend on any specific hosting model (like ASP.NET), but you may design the API so it can integrate with Dependency Injection (IServiceCollection) if available.

2. Connection & Channel Abstraction
   - Implement a connection manager that handles:
     - Creating a connection and channel using RabbitMQ.Client.
     - Reusing the connection and channel.
     - Proper disposal.
   - Example interfaces:
     - IRabbitMqConnectionFactory
     - IRabbitMqChannelProvider

3. Producer API
   - Define an interface, e.g.:

     public interface IRabbitMqProducer
     {
         Task PublishAsync<T>(T message, string? routingKeyOverride = null, CancellationToken cancellationToken = default);
     }

   - Implementation details:
     - Serialize messages to JSON (e.g., using System.Text.Json).
     - Publish to the configured exchange.
     - Use the configured routing key by default (allow override via parameter).
     - Set message as persistent.
     - Handle basic error logging (throw exceptions, or at least log meaningful messages).

4. Consumer API
   - Define an interface that allows registering message handlers, e.g.:

     public interface IRabbitMqConsumer
     {
         void StartConsuming<T>(Func<T, CancellationToken, Task> handler, string? queueNameOverride = null);
         void StopConsuming();
     }

   - Implementation details:
     - Use a basic consumer from RabbitMQ.Client (EventingBasicConsumer).
     - Deserialize message from JSON into T.
     - For each message:
       - Call the handler delegate.
       - On success, ACK the message.
       - On failure, NACK with requeue = true (or configurable).
     - Support graceful shutdown via StopConsuming.

5. Logging
   - Use Microsoft.Extensions.Logging.Abstractions or a simple pluggable logger interface:
     - Prefer: ILogger<SomeType> if you wire DI support.
     - At minimum, log:
       - Connection failures
       - Publish failures
       - Consumer errors
   - The DLL itself should not FORCE a specific logging implementation; just use abstractions.

6. DI-Friendly Setup (Optional but preferred)
   - Provide an extension method for IServiceCollection, e.g.:

     public static class RabbitMqServiceCollectionExtensions
     {
         public static IServiceCollection AddRabbitMqMessaging(
             this IServiceCollection services,
             Action<RabbitMqOptions> configureOptions);
     }

   - This method:
     - Registers RabbitMqOptions.
     - Registers connection factory / channel provider.
     - Registers IRabbitMqProducer / IRabbitMqConsumer implementations.

RabbitMqMessaging.Sample (Console App) – Requirements
=====================================================
Create a simple console application that references the RabbitMqMessaging DLL and demonstrates real usage.

1. Configuration
   - Use an appsettings.json file with sample RabbitMqOptions values, e.g.:

     {
       "RabbitMq": {
         "HostName": "localhost",
         "Port": 5672,
         "VirtualHost": "/",
         "UserName": "guest",
         "Password": "guest",
         "ExchangeName": "demo.exchange",
         "ExchangeType": "topic",
         "QueueName": "demo.queue",
         "RoutingKey": "demo.routing"
       }
     }

   - In the console app, load configuration (e.g., using Microsoft.Extensions.Configuration) and pass the options to the messaging library.

2. Sample Producer
   - Demonstrate sending a typed message, e.g.:

     public class DemoMessage
     {
         public Guid Id { get; set; }
         public string Content { get; set; }
         public DateTime CreatedAt { get; set; }
     }

   - From Main (or a dedicated command), send a few DemoMessage instances using IRabbitMqProducer.

3. Sample Consumer
   - Demonstrate starting a consumer that listens on the configured queue and prints the messages to the console.
   - Show how to wire a handler like:

     async Task HandleDemoMessage(DemoMessage msg, CancellationToken ct)
     {
         Console.WriteLine($"Received message: {msg.Id} - {msg.Content} at {msg.CreatedAt}");
         await Task.CompletedTask;
     }

4. Running
   - In README.md, document how to:
     - Run RabbitMQ (assume standard local RabbitMQ or Docker with ports 5672/15672).
     - Run the sample app to publish and consume messages.

General Coding & Quality Guidelines
===================================
- Use async/await throughout (no blocking .Result / .Wait()).
- Use nullable reference types and treat warnings as important.
- Include basic XML documentation comments on public interfaces and main classes.
- Keep the code clean and organized into folders:
  - Options
  - Connections
  - Producer
  - Consumer
  - Extensions
- Do NOT include Dockerfiles, Kubernetes manifests, or heavy infrastructure. Focus on the .NET and RabbitMQ code itself.
- Ensure the solution builds successfully with a single command (e.g., `dotnet build`).

Deliverables
============
1. Full .NET solution & projects:
   - RabbitMqMessaging (class library → DLL)
   - RabbitMqMessaging.Sample (console app)
2. All source code files.
3. appsettings.json example for the sample app.
4. README.md explaining:
   - Solution structure.
   - How to configure RabbitMQ address.
   - How to run the sample producer/consumer.
   - How to reference the DLL from another project.

Make sure the final result is production-ready enough to be reused in other projects by simply referencing the RabbitMqMessaging DLL and configuring RabbitMqOptions.
