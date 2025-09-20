# **Franz.Common.Messaging.Hosting**

A foundational library within the **Franz Framework** designed to enable and manage hosted messaging services in distributed applications. This package provides utilities for message execution strategies, delegation, transaction management, and seamless integration with **Microsoft.Extensions.Hosting**.

---

## **Features**

- **Hosted Messaging Services**:
  - `MessagingHostedService` for managing message processing as a hosted background service.
- **Message Context Management**:
  - `MessageContextAccessor` for handling message-specific contexts during execution.
- **Delegating Message Actions**:
  - Interfaces and classes such as `IAsyncMessageActionFilter` and `MessageActionExecutionDelegate` for extending and controlling the message processing pipeline.
- **Transaction Management**:
  - `TransactionFilter` ensures consistency in messaging transactions.
- **Messaging Execution Strategies**:
  - `IMessagingStrategyExecuter` to define and execute custom strategies for message processing.
- **Extensions**:
  - `HostBuilderExtensions` for integrating messaging services with the application host builder.

---

## **Version Information**

- **Current Version**: 1.4.4
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0): Simplifies dependency injection configurations.
- **Microsoft.Extensions.Hosting** (8.0.0): Provides hosting abstractions for background services.
- **Franz.Common.Hosting**: Provides general hosting utilities.
- **Franz.Common.Logging**: Enables centralized logging for hosted messaging.
- **Franz.Common.Messaging**: Core messaging utilities and abstractions.

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
dotnet add package Franz.Common.Messaging.Hosting  
```

---

## **Usage**

### **1. Register Messaging Hosted Services**

Use `HostBuilderExtensions` to add hosted messaging services:

```csharp
using Franz.Common.Messaging.Hosting.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .AddMessagingHostedService()
    .Build();

await host.RunAsync();
```

### **2. Customize Messaging Actions**

Extend and control message processing pipelines using `IAsyncMessageActionFilter`:

```csharp
using Franz.Common.Messaging.Hosting.Delegating;

public class CustomMessageActionFilter : IAsyncMessageActionFilter
{
    public async Task OnMessageExecutingAsync(MessageActionExecutingContext context)
    {
        // Add custom logic before message processing
    }

    public async Task OnMessageExecutedAsync(MessageActionExecutedContext context)
    {
        // Add custom logic after message processing
    }
}
```

### **3. Transaction Management**

Ensure transactional consistency using the `TransactionFilter`:

```csharp
using Franz.Common.Messaging.Hosting.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

### **4. Implement Messaging Strategies**

Define custom strategies for message execution using `IMessagingStrategyExecuter`:

```csharp
using Franz.Common.Messaging.Hosting.Executing;

public class CustomMessagingStrategyExecuter : IMessagingStrategyExecuter
{
    public async Task ExecuteAsync(MessageContext context)
    {
        // Custom message execution logic
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.Hosting** package integrates seamlessly with:
- **Franz.Common.Hosting**: General hosting utilities for ASP.NET Core applications.
- **Franz.Common.Logging**: Provides centralized logging for hosted services.
- **Franz.Common.Messaging**: Core messaging utilities for distributed systems.

Ensure these dependencies are installed to leverage the package's full potential.

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
- Upgrade version to .net 9


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**