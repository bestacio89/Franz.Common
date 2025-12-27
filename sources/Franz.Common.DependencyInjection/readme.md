# **Franz.Common.DependencyInjection**

A core library within the **Franz Framework**, designed to simplify and enhance the usage of dependency injection in .NET applications. This package provides utilities and extensions for efficient registration of services, managing lifetimes, and ensuring clean dependency resolution.

---

## **Features**

### **1. Service Registration**
- **Scoped Dependencies**:
  - Automatically registers services implementing `IScopedDependency` with scoped lifetimes.
- **Singleton Dependencies**:
  - Registers services implementing `ISingletonDependency` with singleton lifetimes.
- **Reflection-Based Registration**:
  - Automatically registers services based on custom reflection rules using `ITypeSourceSelectorExtensions`.

### **2. Custom Strategies**
- **RegistrationStrategySkipExistingPair**:
  - Prevents duplicate service registrations, ensuring efficient dependency management.

### **3. Modular Integration**
- **Advanced Assembly Filtering**:
  - Filters assemblies using `FromCompanyApplicationDependenciesWithPredicate` for targeted registration.
- **Flexible Service Types**:
  - Supports scoped, singleton, and transient registrations with various interfaces and class hierarchies.

---

## **Version Information**

- **Current Version**: 1.7.4
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.DependencyInjection.Abstractions**:
  Provides the core interfaces for dependency injection.
- **Scrutor** (4.2.2):
  Enables advanced scanning and automatic service registration.
- **Franz.Common**:
  Supplies core utilities for the framework.
- **Franz.Common.Reflection**:
  Adds reflection utilities for enhanced DI support.

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
dotnet add package Franz.Common.DependencyInjection  
```

---

## **Usage**

### **1. Core Service Registration**

Use the `ServiceCollectionExtensions` to register common dependencies based on their interfaces and lifetimes:

#### Register Scoped Dependencies
Automatically register all classes implementing `IScopedDependency`:
```csharp
using Franz.Common.DependencyInjection.Extensions;

services.AddScopedDependencies();
```

#### Register Singleton Dependencies
Automatically register all classes implementing `ISingletonDependency`:
```csharp
using Franz.Common.DependencyInjection.Extensions;

services.AddSingletonDependencies();
```

#### Add Dependencies with Custom Assembly Filtering
Use a custom predicate to filter assemblies:
```csharp
services.AddDependencies(assembly => assembly.GetName().Name.Contains("MyApp"));
```

---

### **2. Advanced Registration Patterns**

#### Prevent Duplicate Registrations
Avoid redundant registrations using `RegistrationStrategySkipExistingPair`:
```csharp
using Franz.Common.DependencyInjection.Extensions;

services.AddWithStrategy<ILogger, Logger>(new RegistrationStrategySkipExistingPair());
```

#### Reflection-Based Registration
Automatically register services based on reflection:
```csharp
services.Scan(scan => scan
    .FromCompanyApplicationDependenciesWithPredicate(assembly => assembly.GetName().Name.StartsWith("MyCompany"))
    .AddClasses(classes => classes.AssignableTo<IService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

#### Custom Registration for Self and Matching Interfaces
Register services with custom rules:
```csharp
services.AddSelfScoped<IOrderService>();
services.AddMatchingInterfaceScoped<IOrderService>();
services.AddImplementedInterfaceSingleton<IOrderService>();
```

---

### **3. Custom Extension Points**

#### Add No-Duplicate Services
Ensure no duplicate services are added:
```csharp
services.AddNoDuplicateScoped<IOrderService, OrderService>();
services.AddNoDuplicateSingleton<ILogger, Logger>();
services.AddNoDuplicateTransient<IService, ServiceImplementation>();
```

#### Add Inherited Class Registration
Register classes inheriting from a specific base type:
```csharp
services.AddInheritedClassSingleton<MyBaseType>();
```

---

## **Integration with Franz Framework**

The **Franz.Common.DependencyInjection** package integrates seamlessly with:
- **Franz.Common**:
  Provides foundational utilities.
- **Franz.Common.Reflection**:
  Enables advanced assembly and type filtering for DI registration.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team. If you are authorized to contribute:
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

### Version 1.3.6

- DI glue isolated here — core is DI-free.
- AddFranzMediator(), AddFranzMessaging(), etc. live here.
- Microsoft DI = optional adapter (others possible).

### Version 1.6.20
- Updated to **.NET 10.0**