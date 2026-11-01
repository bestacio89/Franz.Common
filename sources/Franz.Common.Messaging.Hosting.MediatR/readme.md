# Franz.Common.Messaging.Hosting.MediatR

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging.Hosting.MediatR` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Hosting.MediatR
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

An extension library within the **Franz Framework** that integrates **MediatR** with hosted messaging services. This package provides support for dispatching messages using **MediatR** pipelines within hosted services, enabling clean, decoupled, and testable messaging workflows.

---

## **Features**

- **MediatR Integration**:
  - Leverages **MediatR** for handling messaging strategies and execution.
- **Hosted Messaging Service Support**:
  - Extends the **Franz.Common.Messaging.Hosting** package to include **MediatR**-based message processing.
- **JSON Serialization**:
  - Supports **Newtonsoft.Json** for handling message payloads during MediatR operations.

---

## **Version Information**

**Current Version:** v2.3.5
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **MediatR** (12.2.0): Provides in-process messaging with pipelines.
- **Newtonsoft.Json** (13.0.3): Facilitates JSON serialization for message payloads.
- **Franz.Common.Messaging.Hosting**: Adds hosting support for messaging workflows.

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
dotnet add package Franz.Common.Messaging.Hosting.MediatR  
```

---

## **Usage**

### **1. Configure MediatR for Hosted Messaging**

Use the `MessagingStrategyExecuter` class to implement MediatR pipelines in hosted messaging workflows:

```csharp
using Franz.Common.Messaging.Hosting.MediatR;

public class MyMessagingStrategyExecuter : IMessagingStrategyExecuter
{
    private readonly IMediator _mediator;

    public MyMessagingStrategyExecuter(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ExecuteAsync(MessageContext context)
    {
        // Dispatch the message using MediatR
        await _mediator.Send(new MyMessageCommand { Payload = context.Message });
    }
}
```

### **2. Register MediatR Services**

Add **MediatR** and messaging services in the `Startup` class:

```csharp
using Franz.Common.Messaging.Hosting.MediatR;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMediatR(typeof(MyMessageHandler).Assembly);
        services.AddMessagingWithMediatR();
    }
}
```

### **3. Implement MediatR Handlers**

Define message handlers using MediatR's `IRequestHandler`:

```csharp
using MediatR;

public class MyMessageCommand : IRequest
{
    public string Payload { get; set; }
}

public class MyMessageHandler : IRequestHandler<MyMessageCommand>
{
    public Task<Unit> Handle(MyMessageCommand request, CancellationToken cancellationToken)
    {
        // Handle the message
        Console.WriteLine($"Processing message: {request.Payload}");
        return Unit.Task;
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.Hosting.MediatR** package integrates seamlessly with:
- **Franz.Common.Messaging.Hosting**: Provides hosted services for messaging workflows.
- **MediatR**: Enables in-process messaging and pipelines.
- **Newtonsoft.Json**: Supports serialization of message payloads.

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
- Added `MessagingStrategyExecuter` for integrating MediatR pipelines into messaging workflows.
- Introduced support for JSON serialization using **Newtonsoft.Json**.
- Full compatibility with **Franz.Common.Messaging.Hosting**.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.6.20
- Updated to **.NET 10.0**

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
