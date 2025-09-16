# **Franz.Common.Messaging.Hosting.MediatR**

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

- **Current Version**: 1.3.3
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