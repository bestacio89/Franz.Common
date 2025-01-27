# **Franz.Common.MultiTenancy**

A lightweight library within the **Franz Framework** designed to enable multi-tenancy support in .NET applications. This package provides interfaces for accessing and managing tenant and domain contexts, enabling seamless integration with multi-tenant architectures.

---

## **Features**

- **Tenant Context Accessor**:
  - `ITenantContextAccessor` interface for retrieving tenant-specific information.
- **Domain Context Accessor**:
  - `IDomainContextAccessor` interface for handling domain-specific data.
- **Extensible Multi-Tenancy**:
  - Provides a foundation for building multi-tenant systems with minimal overhead.

---

## **Version Information**

- **Current Version**: 1.2.62
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package does not depend on external libraries and is designed to be easily integrated into any .NET project.

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
dotnet add package Franz.Common.MultiTenancy --Version 1.2.63
```

---

## **Usage**

### **1. Implement Tenant and Domain Context Accessors**

Provide implementations for the `ITenantContextAccessor` and `IDomainContextAccessor` interfaces:

```csharp
using Franz.Common.MultiTenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    public string TenantId { get; set; } = "DefaultTenant";
}

public class DomainContextAccessor : IDomainContextAccessor
{
    public string DomainName { get; set; } = "DefaultDomain";
}
```

### **2. Register Accessors with Dependency Injection**

Add the context accessors to your service collection:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<IDomainContextAccessor, DomainContextAccessor>();
    }
}
```

### **3. Access Tenant and Domain Information**

Inject the accessors into your services to retrieve tenant and domain-specific data:

```csharp
using Franz.Common.MultiTenancy;

public class MyService
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IDomainContextAccessor _domainContextAccessor;

    public MyService(ITenantContextAccessor tenantContextAccessor, IDomainContextAccessor domainContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
        _domainContextAccessor = domainContextAccessor;
    }

    public void DisplayContextInfo()
    {
        Console.WriteLine($"Tenant ID: {_tenantContextAccessor.TenantId}");
        Console.WriteLine($"Domain Name: {_domainContextAccessor.DomainName}");
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.MultiTenancy** package serves as a foundational utility for enabling multi-tenancy in any **Franz Framework**-based application. Use it alongside other libraries in the framework to build fully multi-tenant-aware systems.

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

### Version 1.2.63
- Added `ITenantContextAccessor` for accessing tenant-specific information.
- Added `IDomainContextAccessor` for retrieving domain-specific data.
- Simplified multi-tenancy support for .NET applications.

