# **Franz.Common.Messaging.MultiTenancy**

A library within the **Franz Framework** that adds multi-tenancy support to messaging workflows. This package provides tools for managing tenant and domain-specific messaging contexts, enabling seamless integration of multi-tenancy in distributed systems.

---

## **Features**

* **Tenant Context Management**:

  * `TenantContextAccessor` for handling tenant-specific data in messaging workflows.
* **Domain Context Management**:

  * `DomainContextAccessor` for managing domain-specific information during message processing.
* **Resolvers**:

  * `HeaderTenantResolver` and `HeaderDomainResolver` for resolving multi-tenancy metadata from message headers.
  * `MessagePropertyTenantResolver` and `MessagePropertyDomainResolver` for resolving multi-tenancy metadata from message properties.
* **Resolution Pipelines**:

  * `DefaultTenantResolutionPipeline` and `DefaultDomainResolutionPipeline` to coordinate multiple resolvers in order.
* **Middleware**:

  * `TenantResolutionMiddleware` and `DomainResolutionMiddleware` for automatic resolution per message.
* **Service Registration**:

  * `ServiceCollectionExtensions` to simplify the setup of multi-tenancy messaging services.
* **Message Builders**:

  * `TenantMessageBuilder` and `DomainMessageBuilder` for constructing messages with tenant and domain data.

---

## **Version Information**

* **Current Version**: 1.5.9
* Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:

* **Franz.Common.Messaging**: Provides foundational messaging utilities and abstractions.
* **Franz.Common.MultiTenancy**: Core utilities for tenant and domain management.

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
dotnet add package Franz.Common.Messaging.MultiTenancy --Version 1.3.1
```

---

## **Usage**

### **1. Register Multi-Tenancy Messaging Services**

Use `ServiceCollectionExtensions` to register messaging multi-tenancy services:

```csharp
using Franz.Common.Messaging.MultiTenancy.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFranzMessagingMultiTenancy();
    }
}
```

---

### **2. Access Tenant and Domain Contexts**

Leverage `TenantContextAccessor` and `DomainContextAccessor` to retrieve tenant and domain-specific data:

```csharp
using Franz.Common.Messaging.MultiTenancy;

public class TenantService
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantService(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public Guid? GetCurrentTenantId() => _tenantContextAccessor.GetCurrentTenantId();
}
```

```csharp
using Franz.Common.Messaging.MultiTenancy;

public class DomainService
{
    private readonly IDomainContextAccessor _domainContextAccessor;

    public DomainService(IDomainContextAccessor domainContextAccessor)
    {
        _domainContextAccessor = domainContextAccessor;
    }

    public Guid? GetCurrentDomainId() => _domainContextAccessor.GetCurrentDomainId();
}
```

---

### **3. Automatic Tenant/Domain Resolution in Messaging Pipelines**

Enable middleware to resolve tenant and domain per-message:

```csharp
using Franz.Common.Messaging.MultiTenancy.Middleware;

public class MessagingPipeline
{
    public void Configure(IMessagePipelineBuilder pipeline)
    {
        pipeline.Use<TenantResolutionMiddleware>();
        pipeline.Use<DomainResolutionMiddleware>();
    }
}
```

---

### **4. Build Tenant and Domain Messages**

Use `TenantMessageBuilder` and `DomainMessageBuilder` to create tenant and domain-aware messages:

```csharp
using Franz.Common.Messaging.MultiTenancy;

var tenantMessage = new TenantMessageBuilder()
    .WithTenantId(Guid.NewGuid())
    .Build();

var domainMessage = new DomainMessageBuilder()
    .WithDomainId(Guid.NewGuid())
    .Build();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.MultiTenancy** package integrates seamlessly with:

* **Franz.Common.Messaging**: Provides core messaging abstractions.
* **Franz.Common.MultiTenancy**: Extends tenant and domain management capabilities into messaging workflows.
* **Franz.Common.Http.MultiTenancy**: Aligns messaging and HTTP multi-tenancy under the same abstractions and contracts.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:

1. Clone the repository. @ [https://github.com/bestacio89/Franz.Common/](https://github.com/bestacio89/Franz.Common/)
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.65

* Added `TenantContextAccessor` and `DomainContextAccessor` for managing tenant and domain contexts.
* Introduced `TenantMessageBuilder` and `DomainMessageBuilder` for constructing tenant and domain-aware messages.
* Integrated `ServiceCollectionExtensions` for streamlined multi-tenancy messaging setup.

### Version 1.3

* Upgraded to **.NET 9.0.8**
* Added **new features and improvements**
* Separated **business concepts** from **mediator concepts**
* Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.3.1

* Expanded **messaging multi-tenancy abstractions** to align with HTTP and core multi-tenancy:

  * Added `HeaderTenantResolver` and `HeaderDomainResolver`.
  * Added `MessagePropertyTenantResolver` and `MessagePropertyDomainResolver`.
  * Introduced `DefaultTenantResolutionPipeline` and `DefaultDomainResolutionPipeline` to orchestrate resolvers.
  * Added `TenantResolutionMiddleware` and `DomainResolutionMiddleware` for automatic resolution in messaging pipelines.
* Enhanced `TenantContextAccessor` and `DomainContextAccessor` to fully implement the updated core contracts (`GetCurrentTenantId`, `SetCurrentTenantId`, etc.).
* Extended `Message` with a `Properties` dictionary for application-level metadata.
* Added `MessagePropertiesExtensions` for strongly-typed property access.
* Improved **service registration** via `AddFranzMessagingMultiTenancy`.

---

