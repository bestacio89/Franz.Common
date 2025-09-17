# **Franz.Common.Messaging.Kafka**

A Kafka integration library within the **Franz Framework** designed to simplify interaction with Kafka topics, producers, and consumers. This package provides tools for creating and managing Kafka connections, handling serialization, and integrating Kafka workflows into distributed systems.

---

## **Features**

- **Kafka Connections**:
  - `ConnectionProvider` and `ConnectionFactoryProvider` for managing Kafka connections and factories.
- **Kafka Consumers**:
  - `KafkaConsumerGroup` and `KafkaConsumerProvider` for managing consumer groups.
- **Modeling**:
  - `KafkaModel` and `ModelProvider` for managing and interacting with Kafka models.
- **Serialization**:
  - Includes `IMessageDeserializer` and `JsonMessageDeserializer` for message serialization and deserialization.
- **Transactions**:
  - Tools like `MessagingTransaction`, `KafkaSender`, and `KafkaConsumerFactory` for handling messaging transactions.
- **Hosting Support**:
  - `Listener` to enable Kafka-based message listening in hosting scenarios.
- **Utilities**:
  - Helper classes like `ExchangeNamer` and `TopicNamer` for topic management.
- **Extensions**:
  - `ServiceCollectionExtensions` for streamlined service registration.

---

## **Version Information**

- **Current Version**:  1.3.8
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Confluent.Kafka** (2.3.0): Provides the core Kafka client functionality.
- **Franz.Common.Annotations**: For custom annotations used in Kafka models.
- **Franz.Common.Hosting**: Enables hosting support for Kafka listeners.
- **Franz.Common.Messaging**: Core messaging utilities and abstractions.
- **Franz.Common.Messaging.Hosting**: Adds hosting integration for messaging workflows.

---

## **Installation**

### **From Private Azure Feed**
Since this package is hosted privately, configure your NuGet client:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.Messaging.Kafka  
```

---

## **Usage**

### **1. Register Kafka Services**

Use `ServiceCollectionExtensions` to register Kafka services:

```csharp
using Franz.Common.Messaging.Kafka.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKafkaMessaging(options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.ClientId = "my-client-id";
        });
    }
}
```

### **2. Consume Kafka Messages**

Set up a Kafka consumer group and process messages:

```csharp
using Franz.Common.Messaging.Kafka.Consumers;

public class KafkaConsumerService
{
    private readonly IKafkaConsumerFactory _consumerFactory;

    public KafkaConsumerService(IKafkaConsumerFactory consumerFactory)
    {
        _consumerFactory = consumerFactory;
    }

    public async Task StartConsumingAsync()
    {
        var consumer = _consumerFactory.CreateConsumer("my-group", "my-topic");
        await foreach (var message in consumer.ConsumeAsync())
        {
            Console.WriteLine($"Received message: {message.Value}");
        }
    }
}
```

### **3. Produce Kafka Messages**

Send messages to a Kafka topic:

```csharp
using Franz.Common.Messaging.Kafka;

public class KafkaProducerService
{
    private readonly KafkaSender _kafkaSender;

    public KafkaProducerService(KafkaSender kafkaSender)
    {
        _kafkaSender = kafkaSender;
    }

    public async Task SendMessageAsync(string topic, string key, string value)
    {
        await _kafkaSender.SendAsync(topic, key, value);
    }
}
```

### **4. Serialize and Deserialize Messages**

Use `JsonMessageDeserializer` for deserializing Kafka messages:

```csharp
using Franz.Common.Messaging.Kafka.Serialisation;

var deserializer = new JsonMessageDeserializer<MyModel>();
var myModel = deserializer.Deserialize(message.Value);
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.Kafka** package integrates seamlessly with:
- **Franz.Common.Messaging**: Provides foundational messaging utilities.
- **Franz.Common.Hosting**: Enables hosting support for Kafka listeners.
- **Confluent.Kafka**: Core Kafka client functionality for producers and consumers.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65
- Added `ConnectionFactoryProvider` and `KafkaConsumerGroup` for connection and consumer management.
- Introduced `KafkaSender` and `KafkaConsumerFactory` for producer and consumer workflows.
- Added serialization utilities with `JsonMessageDeserializer`.
- Full integration with **Franz.Common.Messaging** and **Franz.Common.Hosting**.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.3.6
- Integrated with Franz.Mediator (no MediatR).
- MessagingPublisher.Publish is now async Task.
- MessagingInitializer scans INotificationHandler<> for events.
- Kafka topics auto-created for all integration events.