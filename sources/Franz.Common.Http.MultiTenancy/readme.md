# **Franz.Common.Http.MultiTenancy**

A library within the **Franz Framework** that extends multi-tenancy support for HTTP-based **ASP.NET Core** applications. This package provides tools for tenant-specific context management, dependency injection, and Swagger documentation enhancements, enabling seamless multi-tenant application development.

---

## **Features**

- **Tenant Context Management**:
  - `TenantContextAccessor` and `DomainContextAccessor` for managing tenant-specific information across HTTP requests.
- **Swagger Documentation**:
  - `AddRequiredHeaderParameter` for including tenant-specific headers in Swagger documentation.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for integrating multi-tenancy into your services and application pipeline.

---

## **Version Information**

- **Current Version**: 1.6.14
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Mvc.Core** (2.2.5): Core MVC functionality for tenant-based HTTP applications.
- **Swashbuckle.AspNetCore.SwaggerGen** (6.5.0): For Swagger generation with multi-tenancy support.
- **Franz.Common.MultiTenancy**: Core multi-tenancy utilities and patterns.
- **Franz.Common.Http.Headers**: Provides header utilities for tenant-specific HTTP headers.
- **Franz.Common.Http**: Core HTTP utilities for multi-tenant scenarios.
- **Franz.Common.Headers**: Core header management utilities.

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
dotnet add package Franz.Common.Http.MultiTenancy  
```

---

## **Usage**

### **1. Register Multi-Tenancy Services**

Use `ServiceCollectionExtensions` to register tenant-specific services:

```csharp
using Franz.Common.Http.MultiTenancy.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMultiTenancy(); // Registers multi-tenancy utilities and services
    }
}
```

### **2. Access Tenant Context**

Access tenant-specific information using `TenantContextAccessor` or `DomainContextAccessor`:

```csharp
using Franz.Common.Http.MultiTenancy;

public class MyService
{
    private readonly TenantContextAccessor _tenantContextAccessor;

    public MyService(TenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public string GetTenantId()
    {
        return _tenantContextAccessor.Tenant?.Id;
    }
}
```

### **3. Enhance Swagger Documentation**

Use `AddRequiredHeaderParameter` to include tenant-specific headers in Swagger UI:

```csharp
using Franz.Common.Http.MultiTenancy.Documentation;

services.AddSwaggerGen(options =>
{
    options.OperationFilter<AddRequiredHeaderParameter>();
});
```

This ensures tenant headers are explicitly documented in API specifications.

---

## **Integration with Franz Framework**

The **Franz.Common.Http.MultiTenancy** package integrates seamlessly with:
- **Franz.Common.MultiTenancy**: Provides foundational multi-tenancy utilities.
- **Franz.Common.Http.Headers**: Enhances tenant-specific header handling.
- **Franz.Common.Http**: Enables tenant-aware HTTP services.
- **Franz.Common.Headers**: Provides shared header utilities for HTTP operations.

Ensure these dependencies are installed to fully leverage the library's capabilities.

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

### Version 1.3.1

- Introduced tenant resolution pipeline (DefaultTenantResolutionPipeline) to orchestrate multiple strategies.
- Added resolvers for different multi-tenancy strategies:
  - HeaderTenantResolver (resolves tenant from request headers).
  - HostTenantResolver (resolves tenant from host/subdomain).
  - QueryStringTenantResolver (resolves tenant from query string).
  - JwtClaimTenantResolver (resolves tenant from JWT claims).

- Implemented TenantResolutionMiddleware for resolving the tenant once per request and storing it in context.
- Enhanced TenantContextAccessor and DomainContextAccessor to use HttpContext.Items for request-scoped tenant/domain storage.
- Added DI extension methods (AddFranzMultiTenancy) for easy registration of multi-tenancy services.
- Added middleware extension (UseFranzMultiTenancy) for seamless integration into the ASP.NET Core pipeline.
- Improved Swagger integration by supporting tenant-specific headers in documentation via AddRequiredHeaderParameter.
- Laid foundation for tenant-aware services (DbContexts, caching, feature flags) by standardizing tenant resolution across HTTP requests.
