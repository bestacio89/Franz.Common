# **Franz.Common.DependencyInjection**

A core library within the **Franz Framework** designed to simplify and enhance the usage of dependency injection in .NET applications. This package provides utilities and extensions for efficient registration of services, resolving dependencies, and managing lifetimes.

---

## **Features**

- **Service Registration**:
  - `ServiceCollectionExtensions` simplifies the registration of common service patterns.
- **Custom Strategies**:
  - `RegistrationStrategySkipExistingPair` prevents duplicate service registrations, ensuring efficient dependency resolution.
- **Reflection Utilities**:
  - `ITypeSourceSelectorExtensions` integrates with reflection-based service registration.
- **Modular Integration**:
  - Works seamlessly with other **Franz Framework** libraries, including `Franz.Common` and `Franz.Common.Reflection`.

---

## **Version Information**

- **Current Version**: 1.2.62
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.DependencyInjection.Abstractions**: Provides the core interfaces for dependency injection.
- **Scrutor** (4.2.2): Enables advanced scanning and automatic service registration.
- **Franz.Common**: Core utilities for the framework.
- **Franz.Common.Reflection**: Reflection utilities for enhanced DI support.

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
dotnet add package Franz.Common.DependencyInjection --version 1.2.62
```

---

## **Usage**

### **1. Service Registration**

Use the `ServiceCollectionExtensions` to streamline the registration of services:

```csharp
using Franz.Common.DependencyInjection.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>(); // Example: Scoped registration
        services.AddSingleton<ILogger, Logger>(); // Example: Singleton registration
    }
}
```

### **2. Prevent Duplicate Registrations**

Use `RegistrationStrategySkipExistingPair` to avoid redundant service registrations:

```csharp
using Franz.Common.DependencyInjection.Extensions;

services.AddWithStrategy<ILogger, Logger>(new RegistrationStrategySkipExistingPair());
```

This ensures the service is only registered if it hasn’t been registered already.

### **3. Reflection-Based Registration**

Automatically register services using reflection with `ITypeSourceSelectorExtensions`:

```csharp
using Franz.Common.DependencyInjection.Extensions;

services.Scan(scan => scan
    .FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
    .AddClasses(classes => classes.AssignableTo<IService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

---

## **Integration with Franz Framework**

The **Franz.Common.DependencyInjection** package integrates seamlessly with:
- **Franz.Common**: Provides foundational utilities.
- **Franz.Common.Reflection**: Adds advanced reflection capabilities for dependency injection.

Ensure these dependencies are installed to leverage full functionality.

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

### Version 1.2.62
- Introduced `ServiceCollectionExtensions` for streamlined service registration.
- Added `RegistrationStrategySkipExistingPair` for efficient DI strategies.
- Integrated `ITypeSourceSelectorExtensions` for reflection-based DI.

