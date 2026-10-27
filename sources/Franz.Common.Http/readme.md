# Franz.Common.Http

> HTTP, identity, and service integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Http` package provides HTTP, identity, and service integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Http
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A library within the **Franz Framework** designed to simplify HTTP-related operations, enhance exception handling, and streamline routing and serialization in **ASP.NET Core** applications. This package provides robust tools for managing errors, configuring application pipelines, and extending routing capabilities.

---

## **Features**

- **Error Management**:
  - `ErrorResponseProvider` and `ExceptionFilter` for centralized error handling.
  - Customizable error response strategies with `IErrorResponseProvider`.
- **HTTP Context Extensions**:
  - Utilities for working with `HttpContext` and application configuration.
- **Routing Extensions**:
  - Parameter transformers for localized or custom routing:
    - `FrenchControllerParameterTransformer`
    - `TranslateControllerParameterTransformer`.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection for HTTP services.
- **Compatibility**:
  - Seamlessly integrates with **Franz.Common**, **Franz.Common.DependencyInjection**, **Franz.Common.Errors**, and **Franz.Common.Serialization**.

---

## **Version Information**

**Current Version:** v2.3.2
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Mvc.NewtonsoftJson** (8.0.0): Enables advanced JSON serialization and deserialization.
- **Franz.Common**: Core utilities for the framework.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection.
- **Franz.Common.Errors**: Standardized error handling.
- **Franz.Common.Serialization**: Custom serialization utilities.

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
dotnet add package Franz.Common.Http  
```

---

## **Usage**

### **1. Centralized Error Handling**

Register the `ErrorResponseProvider` and `ExceptionFilter` to handle errors consistently:

```csharp
using Franz.Common.Http.Errors;

services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>(); // Register the exception filter
});

services.AddSingleton<IErrorResponseProvider, ErrorResponseProvider>();
```

### **2. HTTP Context Extensions**

Use `HttpContextExtensions` to simplify access to `HttpContext` data:

```csharp
using Franz.Common.Http.Extensions;

var userAgent = context.GetHeaderValue("User-Agent");
```

### **3. Routing Parameter Transformers**

Implement custom routing logic with parameter transformers:

```csharp
using Franz.Common.Http.Routing;

public class CustomRouting : FrenchControllerParameterTransformer
{
    public override string TransformOutbound(object value)
    {
        return base.TransformOutbound(value)?.ToUpperInvariant(); // Custom transformation logic
    }
}
```

Register the transformer in your `Startup.cs`:

```csharp
services.AddRouting(options =>
{
    options.ConstraintMap.Add("custom", typeof(CustomRouting));
});
```

### **4. Application Pipeline Configuration**

Use `ApplicationBuilderExtensions` to enhance the middleware pipeline:

```csharp
using Franz.Common.Http.Extensions;

app.UseCustomErrorHandling(); // Custom extension for error handling
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http** package integrates seamlessly with:
- **Franz.Common.Errors**: For centralized error handling.
- **Franz.Common.DependencyInjection**: Simplifies DI setup for HTTP services.
- **Franz.Common.Serialization**: Enhances JSON serialization and deserialization.
- **Franz.Common**: Provides foundational utilities.

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

### Version 1.3.6
- Compatible with Franz 1.3.6 stack.
- Self-contained middleware bootstrap (UseHttpArchitecture).
- Swagger & pipeline setup hidden behind Franz extensions.

### Version 1.6.20
- Updated to **.NET 10.0**

### v2.0.1 – Internal Modernization

- Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
- All APIs remain fully backward compatible.
- Tests, listeners, and pipeline components modernized.

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
