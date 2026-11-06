# Franz.Common.Messaging.Identity

> Identity metadata propagation across message publishing and consumption, preserving authenticated execution context.

## Overview

Identity metadata propagation across message publishing and consumption, preserving authenticated execution context.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Identity
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A library within the **Franz Framework** designed to integrate identity management with messaging workflows. This package provides tools for handling authorization contexts, building identity-based message structures, and ensuring seamless communication between identity and messaging services.

---

## **Features**

- **Identity Context Integration**:
  - `IdentityContextAccessor` for managing identity-specific data within messaging workflows.
- **Authorization Message Builder**:
  - `AuthorizationMessageBuilder` for constructing messages with embedded authorization data.
- **Service Registration**:
  - `ServiceCollectionExtensions` for simplifying the setup of identity-aware messaging services.

---

## **Version Information**

**Current Version:** v3.0.0
- Part of the open-source Franz.Common ecosystem.

---

## **Dependencies**

This package relies on:
- **Franz.Common.Headers**: For managing and propagating headers in messaging workflows.
- **Franz.Common.Identity**: Core utilities for identity and authorization management.
- **Franz.Common.Messaging**: Provides foundational messaging utilities and abstractions.

---

## **Installation**

### Install from NuGet.org

```bash
dotnet add package Franz.Common.Messaging.Identity
```

```

Install the package:

```bash
dotnet add package Franz.Common.Messaging.Identity
```

---

## **Usage**

### **1. Access Identity Context in Messaging**

Use `IdentityContextAccessor` to retrieve identity-related data in messaging workflows:

```csharp
using Franz.Common.Messaging.Identity;

public class MyService
{
    private readonly IdentityContextAccessor _identityContextAccessor;

    public MyService(IdentityContextAccessor identityContextAccessor)
    {
        _identityContextAccessor = identityContextAccessor;
    }

    public string GetUserId()
    {
        return _identityContextAccessor.User?.FindFirst("sub")?.Value;
    }
}
```

### **2. Build Authorization Messages**

Leverage `AuthorizationMessageBuilder` to create messages with embedded authorization data:

```csharp
using Franz.Common.Messaging.Identity;

var builder = new AuthorizationMessageBuilder();
var message = builder.WithUserId("12345")
                     .WithRoles(new[] { "Admin", "User" })
                     .Build();
```

### **3. Register Identity-Aware Messaging Services**

Use `ServiceCollectionExtensions` to register identity-aware messaging components:

```csharp
using Franz.Common.Messaging.Identity.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMessagingWithIdentity();
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Messaging.Identity** package integrates seamlessly with:
- **Franz.Common.Messaging**: Provides messaging abstractions and utilities.
- **Franz.Common.Identity**: Extends identity and authorization support into messaging workflows.
- **Franz.Common.Headers**: Facilitates header management for identity-based messages.

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

### Version 1.2.65
- Introduced `IdentityContextAccessor` for managing identity contexts in messaging workflows.
- Added `AuthorizationMessageBuilder` for constructing identity-aware messages.
- Integrated `ServiceCollectionExtensions` for simplified registration of identity-aware messaging services.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.
