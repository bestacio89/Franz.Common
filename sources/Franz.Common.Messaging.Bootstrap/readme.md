# Franz.Common.Messaging.Bootstrap

> Messaging and distributed integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Messaging.Bootstrap` package provides messaging and distributed integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Messaging.Bootstrap
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

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

**Current Version:** v2.3.0
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

### Version 1.6.20
- Updated to **.NET 10.0**


### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
