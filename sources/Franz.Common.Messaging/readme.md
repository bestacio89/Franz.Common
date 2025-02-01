# **Franz.Common.Messaging**

A messaging abstraction library within the **Franz Framework** that simplifies the handling of messages, headers, and events for distributed systems. This package provides a comprehensive set of tools to build, send, and manage messages, with strong support for context propagation, delegation, and customizable strategies.

---

## **Features**

- **Messaging Context Management**:
  - Interfaces like `IMessageContext` and `IMessageContextAccessor` for managing message-specific data.
  - `MessageContext` for centralized context handling.
- **Delegating Handlers**:
  - `MessageBuilderDelegatingHandler` to extend messaging pipelines.
- **Factories**:
  - `MessageFactory` and builder strategies (`CommandMessageBuilderStrategy`, `IntegrationEventMessageBuilderStrategy`) for consistent message construction.
- **Headers**:
  - Tools like `HeaderContextAccessor`, `HeaderNamer`, and `MessageHeaders` to manage and propagate headers.
- **Messaging Lifecycle**:
  - Interfaces for key messaging operations, such as:
    - `IMessagingInitializer`
    - `IMessagingPublisher`
    - `IMessagingSender`
    - `IMessagingTransaction`
- **Constants and Utilities**:
  - Includes `MessagingConstants` and helper classes for common messaging patterns.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for streamlined registration of messaging-related services.

---

## **Version Information**

- **Current Version**: 1.2.64
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.Options** (8.0.0): Provides options pattern for configurations.
- **Microsoft.Extensions.Options.ConfigurationExtensions** (8.0.0): Extends options for configuration binding.
- **Microsoft.Extensions.Primitives** (8.0.0): Supports header and data propagation.
- **Newtonsoft.Json** (13.0.3): For serialization of message payloads.
- **Franz.Common.Business**: Provides core business utilities.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection.
- **Franz.Common.Errors**: For error handling in messaging workflows.
- **Franz.Common.Headers**: Enhances header handling.

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
dotnet add package Franz.Common.Messaging --Version 1.2.64
```

---

## **Usage**

### **1. Configure Messaging Options**

Use `MessagingOptions` to configure messaging behavior:

```csharp
using Franz.Common.Messaging.Configuration;

services.Configure<MessagingOptions>(options =>
{
    options.DefaultExchange = "my-default-exchange";
});
```

### **2. Build and Send Messages**

Leverage `MessageFactory` and `IMessagingSender` to build and send messages:

```csharp
using Franz.Common.Messaging.Factories;

var messageFactory = new MessageFactory();
var message = messageFactory.CreateMessage("my-event", new { Key = "Value" });

await messagingSender.SendAsync(message);
```

### **3. Use Context Accessors**

Access messaging context and headers using `MessageContext`:

```csharp
using Franz.Common.Messaging.Contexting;

public class MyService
{
    private readonly IMessageContextAccessor _messageContextAccessor;

    public MyService(IMessageContextAccessor messageContextAccessor)
    {
        _messageContextAccessor = messageContextAccessor;
    }

    public string GetHeaderValue(string headerKey)
    {
        return _messageContextAccessor.MessageContext.Headers[headerKey];
    }
}
```

### **4. Propagate Headers**

Use `HeaderPropagationMessageBuilder` to ensure headers are propagated in distributed messaging:

```csharp
using Franz.Common.Messaging.Headers;

var headers = new MessageHeaders();
headers.Add("CorrelationId", Guid.NewGuid().ToString());

var messageBuilder = new HeaderPropagationMessageBuilder(headers);
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging** package integrates seamlessly with:
- **Franz.Common.Business**: Provides foundational utilities for business logic.
- **Franz.Common.Headers**: Simplifies header propagation.
- **Franz.Common.DependencyInjection**: Enables streamlined dependency injection for messaging services.

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

### Version 1.2.64
- Upgrade version to .net 9

