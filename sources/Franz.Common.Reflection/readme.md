# **Franz.Common.Reflection**

A utility library within the **Franz Framework** that provides reflection-based tools to simplify and enhance application development. This package includes helper classes, extensions, and abstractions for working with assemblies, types, and dependency injection.

---

## **Features**

- **Assembly Utilities**:
  - `AssemblyWrapper` and `AssemblyAccessorWrapper` for wrapping and accessing assembly details.
  - `ReflectionHelper` for simplifying reflection-based operations.
- **Interface Abstractions**:
  - `IAssembly` and `IAssemblyAccessor` to standardize assembly interactions.
- **Extension Methods**:
  - `AssemblyExtensions`, `TypeExtensions`, `ServiceCollectionExtensions`, and `HostBuilderExtensions` for extending reflection and dependency injection capabilities.
- **Flexible Integration**:
  - Provides a foundation for working with types and assemblies dynamically, enabling runtime configurations.

---

## **Version Information**

- **Current Version**: 1.6.15
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package has no additional external dependencies, making it lightweight and easy to integrate.

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
dotnet add package Franz.Common.Reflection  
```

---

## **Usage**

### **1. Work with Assemblies**

Leverage `AssemblyWrapper` to interact with assemblies:

```csharp
using Franz.Common.Reflection;

var assemblyWrapper = new AssemblyWrapper(typeof(MyClass).Assembly);
var types = assemblyWrapper.GetExportedTypes();
foreach (var type in types)
{
    Console.WriteLine(type.Name);
}
```

### **2. Extend Types**

Use `TypeExtensions` to simplify type-related operations:

```csharp
using Franz.Common.Reflection.Extensions;

var properties = typeof(MyClass).GetPublicProperties();
foreach (var property in properties)
{
    Console.WriteLine(property.Name);
}
```

### **3. Register Dependencies Dynamically**

Use `ServiceCollectionExtensions` to register types dynamically:

```csharp
using Franz.Common.Reflection.Extensions;

services.RegisterAssemblyTypes(typeof(MyClass).Assembly, type =>
{
    return type.IsClass && !type.IsAbstract;
});
```

### **4. Build Hosts Dynamically**

Use `HostBuilderExtensions` to dynamically configure host builders:

```csharp
using Franz.Common.Reflection.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureDynamicServices(services =>
    {
        services.RegisterAssemblyTypes(typeof(MyClass).Assembly);
    })
    .Build();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Reflection** package integrates seamlessly with the **Franz Framework**, providing foundational utilities for dynamic assembly and type handling. It is especially useful in scenarios where runtime configurations or advanced dependency injection are required.

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
- Added `AssemblyWrapper` and `AssemblyAccessorWrapper` for assembly management.
- Introduced `ReflectionHelper` for simplifying reflection-based tasks.
- Provided `TypeExtensions` for enhanced type operations.
- Added `ServiceCollectionExtensions` and `HostBuilderExtensions` for dynamic dependency injection and host configuration.

---


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**
