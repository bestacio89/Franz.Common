# Franz.Common.Headers

> Provides Headers integration and reusable abstractions for .NET applications built with Franz.Common.

## Overview

Provides Headers integration and reusable abstractions for .NET applications built with Franz.Common.

## Installation

```bash
dotnet add package Franz.Common.Headers
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

Here’s the README file tailored for the **Franz.Common.Headers** package based on its structure and dependencies:

---

# **Franz.Common.Headers**

A utility library within the **Franz Framework** designed to simplify and standardize header management in .NET applications. This package integrates with the dependency injection system to ensure consistent handling of headers across services and middleware.

---

## **Features**

- **Header Management**:
  - Tools and utilities for managing HTTP headers in applications.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection setup for header management.
- **Centralized Configuration**:
  - Supports resource-based configurations using `Resources.resx`.
- **Compatibility**:
  - Seamless integration with **Franz.Common** and **Franz.Common.DependencyInjection**.

---

## **Version Information**

**Current Version:** v2.3.4
- Part of the open-source Franz.Common ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.Primitives** (8.0.0): Utilities for working with string tokens and collections.
- **Franz.Common**: Core utilities for the framework.
- **Franz.Common.DependencyInjection**: Simplified DI patterns and service extensions.

---

## **Installation**

### Install from NuGet.org

```bash
dotnet add package Franz.Common.Headers
```

```

Install the package:

```bash
dotnet add package Franz.Common.Headers
```

---

## **Usage**

### **1. Dependency Injection**

Register header-related services using the `ServiceCollectionExtensions`:

```csharp
using Franz.Common.Headers.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHeaderManagement(); // Registers header utilities and related services
    }
}
```

### **2. Access Headers in Middleware or Services**

Use the registered services to access and manage headers consistently:

```csharp
public class MyHeaderService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MyHeaderService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetHeaderValue(string headerName)
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[headerName].ToString();
    }
}
```

### **3. Resource-Based Configurations**

Utilize `Resources.resx` to centralize header key definitions:

```csharp
var customHeaderKey = Properties.Resources.CustomHeaderKey; // Retrieve header key from resources
```

---

## **Integration with Franz Framework**

The **Franz.Common.Headers** package integrates seamlessly with:
- **Franz.Common**: Core utilities for shared functionality.
- **Franz.Common.DependencyInjection**: Simplifies the DI setup for header management services.

Ensure these dependencies are installed to fully utilize the library.

---

## **Contributing**

Contributions are welcome under the MIT License. To propose a change:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request for review.

---

## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.

## **Changelog**

- Upgrade version to .net 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
