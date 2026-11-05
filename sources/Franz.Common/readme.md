# Franz.Common

> Provides Franz Common integration and reusable abstractions for .NET applications built with Franz.Common.

## Overview

Provides Franz Common integration and reusable abstractions for .NET applications built with Franz.Common.

## Installation

```bash
dotnet add package Franz.Common
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A foundational library in the **Franz Framework** designed to provide core utilities, dependency injection abstractions, and common extensions for .NET applications. This library is part of the open-source Franz.Common ecosystem ecosystem, versioned as `1.2.65`, and hosted on a NuGet.org.

---
**Current Version:** v2.3.4
---
## **Features**

### **Dependency Injection Interfaces**
- **`IScopedDependency`**: Marker interface for services with a scoped lifetime.
- **`ISingletonDependency`**: Marker interface for services with a singleton lifetime.

**Important**: Any service interface must inherit from either `IScopedDependency` or `ISingletonDependency`. Services that do not implement one of these interfaces will not be registered properly in the dependency injection system.

### **Data Seeding**
- **`ISeeder`**: Interface for implementing database seeding logic.
  - Includes:
    - **`Order`**: Determines the execution order of seeders.
    - **`SeedAsync()`**: Defines the asynchronous method for data initialization.

### **Extensions**
- **`CollectionExtensions`**:
  - Includes methods for manipulating and interacting with collections:
    - `AddIfNotContains`
    - `AddRange`
    - `IsNullOrEmpty`
- **`EnumerableExtensions`**:
  - Enhances LINQ functionality with methods such as `ForEach`.
- **`HostEnvironmentExtensions`**:
  - Helps determine the current environment:
    - `IsIntegration`
    - `IsValidation`
    - `IsPreProduction`

### **Core Utilities**
- **`Company`**: Provides foundational business logic for managing company-related data.
- **`ProductGeneration`**: Handles logic for generating product-related data.

---

## **Version Information**

- **Current Version**: 2.3.4
- Part of the open-source Franz.Common ecosystem suite, hosted on a NuGet.org.

---

## **Installation**

### Install from NuGet.org

```bash
dotnet add package Franz.Common
```

zure feed:


### **Step 2: Install the Package**
Use the .NET CLI to install:

```bash
dotnet add package Franz.Common
```

---

## **Usage**

### **1. Dependency Injection**

Use `IScopedDependency` and `ISingletonDependency` to simplify service registrations. **Ensure that your service interfaces implement one of these dependencies.**

#### Scoped Service Example:
```csharp
public interface IScopedExampleService : IScopedDependency
{
    void PerformScopedOperation();
}

public class ScopedExampleService : IScopedExampleService
{
    public void PerformScopedOperation()
    {
        Console.WriteLine("Scoped operation executed.");
    }
}

// Registration
services.AddScoped<IScopedExampleService, ScopedExampleService>();
```

#### Singleton Service Example:
```csharp
public interface ISingletonExampleService : ISingletonDependency
{
    string GetConfigValue();
}

public class SingletonExampleService : ISingletonExampleService
{
    public string GetConfigValue() => "Singleton Config";
}

// Registration
services.AddSingleton<ISingletonExampleService, SingletonExampleService>();
```

### **2. Database Seeding**

Implement the `ISeeder` interface for database initialization:

```csharp
public class ExampleSeeder : ISeeder
{
    public int Order => 1;

    public async Task SeedAsync()
    {
        Console.WriteLine("Seeding database...");
        // Add your data seeding logic here
        await Task.CompletedTask;
    }
}

// Example usage
var seeders = serviceProvider.GetServices<ISeeder>()
                             .OrderBy(seeder => seeder.Order);

foreach (var seeder in seeders)
{
    await seeder.SeedAsync();
}
```

### **3. Extensions**

#### Collection Extensions:
```csharp
var list = new List<int> { 1, 2, 3 };

// Check if list is null or empty
if (!list.IsNullOrEmpty())
{
    Console.WriteLine("List contains elements.");
}

// Add an item if it doesn't exist
list.AddIfNotContains(4); // Adds 4
list.AddIfNotContains(1); // Does nothing

// Add a range of items
list.AddRange(new[] { 5, 6 });
```

#### Enumerable Extensions:
```csharp
var numbers = Enumerable.Range(1, 5);

// Perform an action for each element
numbers.ForEach(x => Console.WriteLine($"Processing {x}"));
```

#### Host Environment Extensions:
```csharp
if (hostEnvironment.IsIntegration())
{
    Console.WriteLine("Running in the integration environment.");
}
else if (hostEnvironment.IsValidation())
{
    Console.WriteLine("Running in the validation environment.");
}
else if (hostEnvironment.IsPreProduction())
{
    Console.WriteLine("Running in the pre-production environment.");
}
```

---

## **Dependencies**

This library is fully self-contained and serves as a core building block for the open-source Franz.Common ecosystem.

---

## **Development Notes**

This package is publicly available on NuGet.org and distributed under the MIT License.

### **Contributing**
Contributions are restricted to the Franz Framework development team. If you are authorized to contribute:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch.
3. Submit a pull request with a detailed explanation of your changes.

---

## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.

## **Changelog**

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**
---

### Version 1.6.20
- Updated to **.NET 10.0**

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
