# Franz.Common.Http.Authentication

> HTTP, identity, and service integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Http.Authentication` package provides HTTP, identity, and service integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Http.Authentication
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A specialized library within the **Franz Framework** that provides streamlined configurations for **JWT Bearer Authentication** and Swagger integration in **ASP.NET Core** applications. This package simplifies secure API development by integrating authentication mechanisms and enhancing API documentation with authentication workflows.

---

## **Features**

- **JWT Bearer Authentication**:
  - Simplifies configuration and registration of JWT Bearer Authentication for secure APIs.
- **Swagger Integration**:
  - Enhances Swagger documentation with JWT support using `Swashbuckle.AspNetCore.SwaggerGen`.
- **Service Registration**:
  - `ServiceCollectionExtensions` for easy setup of authentication services and Swagger enhancements.

---

## **Version Information**

**Current Version:** v2.3.3
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.0): Provides middleware for JWT authentication in ASP.NET Core.
- **Swashbuckle.AspNetCore.SwaggerGen** (6.5.0): Adds Swagger generation capabilities for APIs with authentication.
- **Microsoft.NETCore.App**: Core framework for .NET applications.

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
dotnet add package Franz.Common.Http.Authentication  
```

---

## **Usage**

### **1. Configuring JWT Bearer Authentication**

Use the `ServiceCollectionExtensions` to configure JWT authentication:

```csharp
using Franz.Common.Http.Authentication.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddJwtBearerAuthentication(options =>
        {
            options.Authority = "https://your-auth-server";
            options.Audience = "your-api-audience";
        });
    }
}
```

This automatically registers the necessary middleware for JWT authentication.

### **2. Swagger Integration with Authentication**

Enable JWT authentication in your Swagger configuration:

```csharp
using Franz.Common.Http.Authentication.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerWithJwtSupport(); // Adds Swagger integration with JWT
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        });
    }
}
```

### **3. Protecting API Endpoints**

Secure your endpoints with `[Authorize]` attributes:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetOrders()
    {
        return Ok(new[] { "Order1", "Order2" });
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Authentication** package integrates seamlessly with:
- **Franz.Common.Http**: Provides complementary HTTP utilities.
- **Franz.Common**: Core utilities for shared functionality.

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


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
