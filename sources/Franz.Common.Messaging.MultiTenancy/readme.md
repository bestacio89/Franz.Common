# **Franz.Common.Messaging.MultiTenancy**

A library within the **Franz Framework** that adds multi-tenancy support to messaging workflows. This package provides tools for managing tenant and domain-specific messaging contexts, enabling seamless integration of multi-tenancy in distributed systems.

---

## **Features**

- **Tenant Context Management**:
  - `TenantContextAccessor` for handling tenant-specific data in messaging workflows.
- **Domain Context Management**:
  - `DomainContextAccessor` for managing domain-specific information during message processing.
- **Message Builders**:
  - `TenantMessageBuilder` and `DomainMessageBuilder` for constructing messages with tenant and domain data.
- **Service Registration**:
  - `ServiceCollectionExtensions` to simplify the setup of multi-tenancy messaging services.

---

## **Version Information**

- **Current Version**: 1.2.62
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Franz.Common.Messaging**: Provides foundational messaging utilities and abstractions.
- **Franz.Common.MultiTenancy**: Core utilities for tenant and domain management.

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
dotnet add package Franz.Common.Messaging.MultiTenancy --version 1.2.62
```

---

## **Usage**

### **1. Register Multi-Tenancy Messaging Services**

Use `ServiceCollectionExtensions` to register multi-tenancy messaging services:

```csharp
using Franz.Common.Messaging.MultiTenancy.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultiTenancyMessaging();
    }
}
```

### **2. Access Tenant and Domain Contexts**

Leverage `TenantContextAccessor` and `DomainContextAccessor` to retrieve tenant and domain-specific data:

```csharp
using Franz.Common.Messaging.MultiTenancy;

public class TenantService
{
    private readonly TenantContextAccessor _tenantContextAccessor;

    public TenantService(TenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public string GetCurrentTenantId()
    {
        return _tenantContextAccessor.TenantId;
    }
}
```

```csharp
using Franz.Common.Messaging.MultiTenancy;

public class DomainService
{
    private readonly DomainContextAccessor _domainContextAccessor;

    public DomainService(DomainContextAccessor domainContextAccessor)
    {
        _domainContextAccessor = domainContextAccessor;
    }

    public string GetCurrentDomainName()
    {
        return _domainContextAccessor.DomainName;
    }
}
```

### **3. Build Tenant and Domain Messages**

Use `TenantMessageBuilder` and `DomainMessageBuilder` to create tenant and domain-aware messages:

```csharp
using Franz.Common.Messaging.MultiTenancy;

var tenantMessageBuilder = new TenantMessageBuilder();
var tenantMessage = tenantMessageBuilder.WithTenantId("tenant-123")
                                        .Build();

var domainMessageBuilder = new DomainMessageBuilder();
var domainMessage = domainMessageBuilder.WithDomainName("example.com")
                                         .Build();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.MultiTenancy** package integrates seamlessly with:
- **Franz.Common.Messaging**: Provides core messaging abstractions.
- **Franz.Common.MultiTenancy**: Extends tenant and domain management capabilities into messaging workflows.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you have access, follow these steps:
1. Clone the repository.
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the MIT License. See the `LICENSE` file for more details.

---

## **Changelog**

### Version 1.2.62
- Added `TenantContextAccessor` and `DomainContextAccessor` for managing tenant and domain contexts.
- Introduced `TenantMessageBuilder` and `DomainMessageBuilder` for constructing tenant and domain-aware messages.
- Integrated `ServiceCollectionExtensions` for streamlined multi-tenancy messaging setup.
