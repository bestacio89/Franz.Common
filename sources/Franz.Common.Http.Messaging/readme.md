# **Franz.Common.Http.Messaging**

A specialized library within the **Franz Framework**, designed to streamline the integration of messaging systems, health checks, and transaction management for HTTP-based services in **ASP.NET Core** applications. This package provides tools to ensure seamless messaging health monitoring and transactional consistency in distributed systems.

---

## **Features**

- **Messaging Health Checks**:
  - `KafkaHealthCheck` for monitoring the health of Kafka messaging systems.
- **Transaction Management**:
  - `TransactionFilter` for ensuring transactional consistency in HTTP requests.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for registering messaging and transaction-related services easily.

---

## **Version Information**

- **Current Version**:  1.3.6
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package depends on the following:
- **Franz.Common.Http**: For HTTP utilities and middleware integration.
- **Franz.Common**: Provides foundational utilities.
- **Franz.Common.Messaging** (if available): Additional messaging-related abstractions and utilities.

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
dotnet add package Franz.Common.Http.Messaging  
```

---

## **Usage**

### **1. Messaging Health Checks**

Register and configure Kafka health checks:

```csharp
using Franz.Common.Http.Messaging.Healthchecks;

services.AddHealthChecks()
    .AddCheck<KafkaHealthCheck>("Kafka");
```

This will monitor the Kafka messaging system's health and integrate it into ASP.NET Core's health check system.

### **2. Transaction Management**

Apply the `TransactionFilter` to ensure transactional consistency:

```csharp
using Franz.Common.Http.Messaging.Transactions;

services.AddControllers(options =>
{
    options.Filters.Add<TransactionFilter>();
});
```

### **3. Dependency Injection**

Register messaging and transaction services with `ServiceCollectionExtensions`:

```csharp
using Franz.Common.Http.Messaging.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMessagingUtilities(); // Registers messaging utilities and services
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Messaging** package integrates seamlessly with:
- **Franz.Common.Http**: Enhances HTTP-based applications with messaging support.
- **Franz.Common**: Provides foundational utilities and patterns.

Ensure these dependencies are installed to fully leverage the library's capabilities.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/ @ https://github.com/bestacio89/Franz.Common/
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
- Upgrade version to .net 9.0.8
- New features and improvements
- Mediator concepts separated and compatible with both custom mediator and Mediatr
