# **Franz.Common**

A foundational library in the **Franz Framework** designed to provide core utilities, dependency injection abstractions, and common extensions for .NET applications. This library is part of the private Franz Framework ecosystem, versioned as `1.2.62`, and hosted on a private Azure NuGet feed.

---

## **Features**

- **Dependency Injection Interfaces**:
  - `IScopedDependency` for scoped lifetime services.
  - `ISingletonDependency` for singleton lifetime services.
- **Data Seeding**:
  - `ISeeder` interface for implementing database seeding logic.
- **Extensions**:
  - `CollectionExtensions` for working with collections.
  - `EnumerableExtensions` for LINQ-related enhancements.
  - `HostEnvironmentExtensions` for interacting with hosting environments.
- **Core Utilities**:
  - `Company` and `ProductGeneration` classes for foundational business logic.

---

## **Version Information**

- **Current Version**: `1.2.62`
- Part of the private Franz Framework suite, hosted on an Azure NuGet feed.

---

## **Installation**

### **Step 1: Add the Private Azure Feed**
Configure your NuGet client to access the private Azure feed:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

### **Step 2: Install the Package**
Use the .NET CLI to install:

```bash
dotnet add package Franz.Common --version 1.2.62
```

---

## **Usage**

### **1. Dependency Injection**

Use `IScopedDependency` and `ISingletonDependency` to simplify service registrations:

```csharp
public class MyService : IScopedDependency
{
    public void DoWork()
    {
        // Business logic here
    }
}
```

Register the service:

```csharp
services.AddScoped<MyService>();
```

### **2. Database Seeding**

Implement the `ISeeder` interface for database initialization:

```csharp
public class MySeeder : ISeeder
{
    public Task SeedAsync()
    {
        // Seeding logic here
        return Task.CompletedTask;
    }
}
```

### **3. Extensions**

Leverage provided extensions for common tasks:

- **Collections**:

```csharp
var myList = new List<int> {1, 2, 3};
myList.AddRangeIfNotExists(new[] {3, 4, 5});
```

- **Enumerable Enhancements**:

```csharp
var numbers = Enumerable.Range(1, 10);
var result = numbers.FilterBy(x => x > 5);
```

- **Host Environment**:

```csharp
var isProduction = hostEnvironment.IsProduction();
```

### **4. Core Utilities**

- **Company Class**: Represents foundational business logic for managing company-related data.
- **ProductGeneration Class**: Handles logic related to generating product data.

---

## **Dependencies**

- Fully independent; designed as a core library for the Franz Framework ecosystem.

---

## **Development Note**

This library is under private development as part of the Franz Framework. It is not available on NuGet.org but is distributed through a private Azure feed.

---

## **Contributing**

Contributions are restricted to the Franz Framework development team. If you have access, follow these steps:
1. Clone the repository.
2. Create a feature branch.
3. Submit a pull request with detailed explanations.

---

## **License**

This library is part of a private framework and subject to internal licensing terms. Contact the author for more details.

---

## **Changelog**

### Version 1.2.62
- Introduced core dependency injection abstractions.
- Added database seeding interface.
- Enhanced collection and enumerable extensions.
- Included foundational `Company` and `ProductGeneration` classes.

---

