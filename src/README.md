# AmqpMessageReceiver

AmqpMessageReceiver is a .NET library for receiving messages from AMQP brokers (such as RabbitMQ, ActiveMQ Artemis). It is designed for seamless integration into .NET applications using Dependency Injection (DI).

## Features

- Easy integration with .NET Dependency Injection (DI)
- Support for AMQP-based brokers (RabbitMQ, ActiveMQ Artemis, etc.)
- Configurable message handling with `IMessageReceiver`
- Customizable options via `MessageReceiverOptions`

## Installation

Install the package via NuGet:

```sh
dotnet add package AmqpClientInject
dotnet add package AmqpMessageReceiver
```

Or via the NuGet Package Manager:

```sh
Install-Package AmqpMessageReceiver
```

## Configuration and Registration

To use `AmqpMessageReceiver`, you need to register it in your DI container with the required configuration.

### Registering the Receiver in DI

In your `Program.cs` or `Startup.cs`, register the receiver:

```csharp
using AmqpMessageReceiver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();
services.AddAmqpClient(options =>
{
    options.Host = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
});
builder.Services.AddMessageReceiver(options =>
{
    options.Address = "my-queue";
    options.ReceiverName = "my-Receiver";
    options.MaxConcurrency = 10;
});

var app = builder.Build();
app.Run();
```

### Implementing a Message Receiver in a Worker Service

Create a `Worker` service that processes messages using `IMessageReceiver` and `IAmqpClient`:

```csharp
using AmqpMessageReceiver;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class Worker(IAmqpClient amqpClient, IMessageReceiver receiver) : BackgroundService
{
    private async Task ProcessMessageAsync(MessageContext context)
    {        
        try
        {
            // Process the message
            context.Acknowledge(true);             
        }
        catch (Exception e)
        {
            context.Acknowledge(false);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {     
        await receiver.ExecuteAsync(amqpClient, ProcessMessageAsync, stoppingToken);        
    }    
}
```

## Contribution & Support

If you have any questions or suggestions, feel free to create an issue or a pull request in the [GitHub repository](https://github.com/Korjn/AmqpMessageReceiver).

## License

This library is distributed under the MIT license.

