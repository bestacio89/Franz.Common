# Franz.Common.Bootstrap

> Shared framework capability for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Bootstrap` package provides the shared framework capability capability within Franz.Common. It is designed to be composed with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Bootstrap
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A foundational library within the **Franz Framework**, designed to simplify the application startup process by centralizing dependency injection, resource management, and configuration. This package integrates seamlessly with other **Franz Framework** libraries to provide a streamlined and modular development experience.

---

## **Features**

- **Service Registration**:
  - `ServiceCollectionExtensions` to bootstrap common dependencies for application startup.
- **Resource Management**:
  - `Resources.resx` for centralized localization and configuration resources.
- **Modular Integration**:
  - Seamlessly integrates with key **Franz Framework** libraries, such as:
    - `Franz.Common.AutoMapper`
    - `Franz.Common.Business`
    - `Franz.Common.DependencyInjection`
    - `Franz.Common.Hosting`
    - `Franz.Common.Logging`

---

## **Version Information**

**Current Version:** v2.3.2
- Part of the private **Franz Framework** ecosystem.

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
dotnet add package Franz.Common.Bootstrap  
```

---

## **Usage**

### **1. Bootstrap Dependencies**

Use the `ServiceCollectionExtensions` to register common dependencies with a single method:

```csharp
using Franz.Common.Bootstrap;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.BootstrapCommonServices(); // Registers shared dependencies like Logging, AutoMapper, Hosting, etc.
    }
}
```

This ensures all key dependencies from the **Franz Framework** are automatically registered.

---

### **2. Resource Management**

Access localized strings or configuration values from `Resources.resx`:

```csharp
var message = Properties.Resources.SomeKey; // Retrieves a value by its key
```

---

## **Dependencies**

The **Franz.Common.Bootstrap** library integrates with and relies on other key **Franz Framework** libraries:

- **Franz.Common.AutoMapper**: For mapping configurations.
- **Franz.Common.Business**: Provides business logic utilities.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection patterns.
- **Franz.Common.Hosting**: Manages hosting-related configurations.
- **Franz.Common.Logging**: Integrates with logging frameworks.

Make sure these packages are installed and accessible within your project if required.

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

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**
---

### Version 1.3.4
- Eliminated dependency on `Franz.Common.Bootstrap` to Automapper

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

Snappier is pinned to a patched version to prevent a vulnerable transitive version from being selected. This does not change the bootstrap API. Restore and tests must validate the resolved graph before publishing. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
