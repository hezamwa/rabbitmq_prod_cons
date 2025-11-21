## RabbitMqMessagingSolution

Reusable RabbitMQ producer/consumer abstractions packaged as a .NET class library plus a sample console application that demonstrates publishing and consuming typed messages.

### Projects
- `RabbitMqMessaging` — class library targeting .NET 10 that exposes configurable connection management, producer, and consumer services built on top of `RabbitMQ.Client`.
- `RabbitMqMessaging.Sample` — console application (also targeting .NET 10) that loads `appsettings.json`, wires up the messaging services via dependency injection, publishes demo messages, and prints the ones consumed from the configured queue.

### Configuration
All RabbitMQ settings live in `appsettings.json` under the `RabbitMq` section. Every setting from `RabbitMqOptions` can be specified: host, port, virtual host, credentials, exchange, exchange type, queue, routing key, and `RequeueOnFailure`.

Example:
```json
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
```

### Running the sample
1. Ensure RabbitMQ is running locally (e.g., Docker `rabbitmq:3-management` with ports `5672:5672` and `15672:15672`).
2. Restore/build the solution:
   ```
   dotnet build RabbitMqMessagingSolution.sln
   ```
3. Run the sample console app:
   ```
   dotnet run --project RabbitMqMessaging.Sample
   ```
4. Follow the prompts to send demo messages and observe the consumer output.

### Reusing the DLL
Reference `RabbitMqMessaging.dll` from any .NET app (Web API, worker service, console, etc.), add the NuGet dependencies noted in the csproj, then configure services:

```csharp
services.AddRabbitMqMessaging(options => configuration.GetSection("RabbitMq").Bind(options));
```

Resolve `IRabbitMqProducer` to publish JSON messages and `IRabbitMqConsumer` to register handlers that ACK/NACK automatically. The library relies only on `Microsoft.Extensions.*` abstractions, so it fits cleanly into any dependency injection setup.

