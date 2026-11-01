# Franz.Common.AutoMapper

> Object mapping for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.AutoMapper` package provides the object mapping capability within Franz.Common. It is designed to be composed with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.AutoMapper
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A utility library within the **Franz Framework** designed to streamline the integration and configuration of **AutoMapper** in .NET applications. This package includes helpful extensions for dependency injection, simplifying the setup and usage of AutoMapper profiles.

---

## **Features**

- **AutoMapper Dependency Injection**:
  - `ServiceCollectionExtensions` to easily register AutoMapper with all your profiles in a single call.
- **Simplified AutoMapper Configuration**:
  - Automatically scan and load mapping profiles for your application.

---

## **Version Information**

- **Current Version**: 2.3.5
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
dotnet add package Franz.Common.AutoMapper  
```

---

## **Usage**

### **1. Register AutoMapper with Dependency Injection**

Use the `ServiceCollectionExtensions` to register all your mapping profiles automatically:

```csharp
using Franz.Common.AutoMapper;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAutoMapperProfiles(); // Automatically loads all profiles
    }
}
```

### **2. Create AutoMapper Profiles**

Define your mapping profiles as usual:

```csharp
using AutoMapper;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderDto, Order>();
    }
}
```

The `AddAutoMapperProfiles` method will scan and register all classes inheriting from `Profile`.

---

## **Dependencies**

This package depends on:
- **AutoMapper** (>= 12.0.0)

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

### Version 1.6.20
- Updated to **.NET 10.0**
