# Avs.Messaging RabbitMq

**Avs.Messaging RabbitMq** is a lightweight, RabbitMq messaging library for .NET 9. It enables decoupled communication between components using message-based interaction. Ideal for small to mid-sized applications needing simple publish-subscribe or command-style messaging.

---

## 🚀 Features

- ✅ RabbitMq transport based on the official .NET RabbitMq client
- 🧩 Supports Pub/Sub, direct, topic and request-reply features
- 🧼 Decouples message senders from handlers
- ✅ Provides a flexible configuration of exchange
- 💡 Built for .NET 9+

---

## Usage

1) Install the latest version of the package from Nuget:

```
dotnet add package Avs.Messaging RabbitMq
```

2) Add your message and message consumer classes:

```csharp
public class Greeting
{
    public string Message { get; set; } = default!;
    public DateTime Time { get; set; } = DateTime.UtcNow;
}

public class GreetingConsumer(ILogger<GreetingConsumer> logger) : ConsumerBase<Greeting>
{
    protected override Task Consume(MessageContext<Greeting> messageContext)
    {
        logger.LogInformation(messageContext.Message);
        
        return Task.CompletedTask;
    }
}
```

3) Configure Avs.Messaging services in the app startup:

```csharp
services.AddMessaging(x =>
{
    x.AddConsumer<GreetingConsumer>(); // this registered GreetingConsumer as a scoped service
    x.UseRabbitMq(cfg =>
    {
         cfg.Host = "localhost";
         cfg.Port = 5672;
         cfg.Username = "guest";
         cfg.Password = "guest";
    });
});

```
If you need some special configuration of exchange, you can use RabbitMqOptions.ConfigureExchange<T>(...) method to override default settings:

```csharp
services.AddMessaging(x =>
{
    x.AddConsumer<GreetingConsumer>(); // this registered GreetingConsumer as a scoped service
    x.UseRabbitMq(cfg =>
    {
         cfg.Host = "localhost";
         cfg.Port = 5672;
         cfg.Username = "guest";
         cfg.Password = "guest";
         cfg.ConfigureExchangeOptions<Greeting>(o =>
         {
             o.ExchangeName = "greetings"; // By default, exchange name is the full name of message type
             o.RoutingKey = "greetings.consumer.*";
             o.SetTopicExchange(); // Set exchange type to topic
         });
    });
});

```

---

Feel free to use this package and create a pull request to this repository.