# 📝 **README Section for `Franz.Common.Messaging.RabbitMQ` (v1.6.2)**

````markdown
# Franz.Common.Messaging.RabbitMQ

Franzchitecture now speaks RabbitMQ.  
The same architectural creed that powers Kafka applies here — one contract, one pipeline, zero spaghetti.

## 🚀 Getting Started

Install the NuGet package:

```bash
dotnet add package Franz.Common.Messaging.RabbitMQ
````

Register RabbitMQ messaging in your service container:

```csharp
using Franz.Common.Messaging.RabbitMQ.Extensions;

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddRabbitMQMessaging(configuration);
}
```

## 📦 Features

* **Publisher & Sender** wired via DI (`IMessagingPublisher`, `IMessagingSender`)
* **Consumer** as a hosted listener (`IListener`)
* **Transactions** (`IMessagingTransaction`)
* **Replay Strategies** (default: `NoReplayStrategy`, extensible)
* **Message Context Accessor** for scoped metadata
* **Enforced lifetimes** via `AddOnlyHighLifetimeModelProvider` to prevent misconfigurations
* **No duplicates** — registrations protected against accidental overrides

## ⚖️ Philosophy

> *“We don’t care about MQ. We make it work anyway.
> Just pick what hill you want to die on.”*

Franz abstracts away broker-specific plumbing. Whether you pick **Kafka** or **RabbitMQ**,
your code stays clean, enforced, and production-ready.


**Current Version**: 1.6.21
---

### Version 1.6.20
- Updated to **.NET 10.0**
- Improved dependency injection patterns for messaging services.
- Enhanced documentation and usage examples.
- Update to RabbitMQ client library to latest stable version.
- Realligned messaging abstractions for better consistency across brokers.