# **Franz.Common.Messaging.Bootstrap**

A modular library within the **Franz Framework** designed to streamline the setup and integration of messaging components in distributed systems. This package provides a centralized configuration for messaging-related services, simplifying dependency injection and ensuring seamless interoperability across other **Franz Framework** messaging libraries.

---

## **Features**

- **Centralized Messaging Configuration**:
  - Simplifies the setup of messaging utilities across projects.
- **Service Registration**:
  - `ServiceCollectionExtensions` to bootstrap messaging services, including hosting, identity, Kafka, and multi-tenancy.
- **Seamless Integration**:
  - Integrates effortlessly with other **Franz Messaging** components such as `Hosting`, `Hosting.MediatR`, `Identity`, `Kafka`, and `MultiTenancy`.

---

## **Version Information**

- **Current Version**: 1.4.2
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package depends on:
- **Franz.Common.Bootstrap**: Provides core bootstrap utilities for application initialization.
- **Franz.Common.Messaging.Hosting**: Enables hosting configurations for messaging services.
- **Franz.Common.Messaging.Hosting.MediatR**: Adds MediatR support for messaging workflows.
- **Franz.Common.Messaging.Identity**: Integrates identity management with messaging.
- **Franz.Common.Messaging.Kafka**: Provides Kafka-specific messaging abstractions and tools.
- **Franz.Common.Messaging.MultiTenancy**: Supports multi-tenant configurations for messaging services.

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
dotnet add package Franz.Common.Messaging.Bootstrap  
```

---

## **Usage**

### **1. Bootstrap Messaging Services**

Leverage `ServiceCollectionExtensions` to register all messaging services:

```csharp
using Franz.Common.Messaging.Bootstrap.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMessagingBootstrap(); // Registers messaging services and dependencies
    }
}
```

This method ensures that all required messaging-related components, including hosting, Kafka, and multi-tenancy, are configured automatically.

### **2. Custom Messaging Configuration**

You can further customize your messaging setup by directly registering specific components:

```csharp
services.AddMessagingHosting();
services.AddMessagingHostingMediatR();
services.AddMessagingKafka();
services.AddMessagingMultiTenancy();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.Bootstrap** package is designed to serve as a central hub for configuring and integrating messaging services within the **Franz Framework**. It seamlessly integrates with:
- **Franz.Common.Bootstrap**
- **Franz.Common.Messaging.Hosting**
- **Franz.Common.Messaging.Identity**
- **Franz.Common.Messaging.Kafka**
- **Franz.Common.Messaging.MultiTenancy**

Ensure these dependencies are installed to fully leverage the package's capabilities.

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
