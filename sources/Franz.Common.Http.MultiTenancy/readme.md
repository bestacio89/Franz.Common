# Franz.Common.Http.MultiTenancy

> HTTP, identity, and service integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Http.MultiTenancy` package provides HTTP, identity, and service integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Http.MultiTenancy
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

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

**Current Version:** v2.3.5
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

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).


## Secure request context

Register the HTTP integration and place its middleware after authentication and before authorization/endpoints:

```csharp
services.AddFranzMultiTenancy(options =>
{
    options.RequireTenant = true; // fail closed when no active tenant resolves
});

app.UseAuthentication();
app.UseFranzMultiTenancy();
app.UseAuthorization();
```

Tenant and domain accessors read server-established values from `HttpContext.Items`; inbound headers are resolution inputs only and are never the authoritative context. The middleware validates tenant lifecycle state, optionally invokes an application-registered `ITenantValidator`, creates an immutable execution context, and clears it after the request.

The JWT claim resolver ignores unauthenticated principals. Register only the resolution strategies appropriate to the application. Query-string and header selection must not be treated as authorization: applications must still authorize the authenticated principal's membership in the resolved tenant.

**Compatibility note:** HTTP accessors no longer read tenant/domain IDs from request headers. Consumers that previously used caller-supplied headers as ambient state must use a trusted resolver and the execution context accessor. `ITenantValidator.IsValidAsync` now accepts a cancellation token.


## Authenticated tenant membership

For authenticated principals, the HTTP tenant middleware requires an application-registered `ITenantRequestAuthorizer`. If no authorizer is registered, or membership is denied, the request is rejected with HTTP 403. Implement this contract against your identity/membership store; a tenant ID claim or request header alone is not membership proof. Anonymous tenant access remains subject to the application's endpoint authorization policy.


## Request resolution is not authorization for 2.3.5

Tenant resolution from a host, route, claim, or header identifies a candidate tenant; it does not establish membership or authorize access by itself. For authenticated principals, register and implement `ITenantRequestAuthorizer`; absent registration or denied membership must fail closed. A tenant identifier supplied by the caller is not membership proof. See the [tenant-isolation audit](../../Docs/tenant-isolation-boundary-audit.md) for the broader framework boundary.

---

## Cancellation and immutable request context

Tenant resolution now carries `HttpContext.RequestAborted` through the resolution pipeline, HTTP resolvers, tenant-store lookup, tenant validation, and authenticated membership authorization. The cancellation-aware `ITenantStore` overloads have default interface implementations for source/binary compatibility; I/O-backed tenant stores should override them and pass the token to their underlying provider.

After resolution and authorization, the middleware establishes one immutable `TenantExecutionContext`. The HTTP tenant/domain accessors reject attempts to replace tenant or domain identity while that validated context is active. The context is cleared in a `finally` block, including when downstream middleware throws or is cancelled.

The authenticated `IdentityContextAccessor` reads user claims only from an authenticated principal. Tenant and domain values come from the validated execution context. Use `IHeaderContextAccessor` to inspect raw headers; never treat caller-supplied identity headers as proof of identity or membership.
