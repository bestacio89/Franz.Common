# **Franz.Common.Hosting**

A utility library within the **Franz Framework** designed to simplify and enhance hosting configurations for .NET applications. This package provides powerful extensions for working with hosts and service providers, offering flexibility and consistency in application startup and lifecycle management.

---

## **Features**

- **Host Utilities**:
  - `HostExtensions` for managing and customizing host configurations.
- **Service Provider Extensions**:
  - `ServiceProviderExtensions` for streamlined service resolution and initialization.
- **Hosting Initialization**:
  - `IHostingInitializer` interface to define and manage hosting initialization logic.
- **Compatibility**:
  - Integrates seamlessly with **Franz.Common**, **Franz.Common.DependencyInjection**, and **Franz.Common.Reflection**.

---

## **Version Information**

- **Current Version**: 1.7.4
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.Hosting.Abstractions** (8.0.0): Core hosting abstractions for building and running .NET applications.
- **Franz.Common**: Provides core utilities and shared functionality.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection patterns.
- **Franz.Common.Reflection**: Offers advanced reflection utilities for host configuration and initialization.

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
dotnet add package Franz.Common.Hosting  
```

---

## **Usage**

### **1. Customize Host Configuration**

Use `HostExtensions` to configure the host:

```csharp
using Franz.Common.Hosting.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register services here
    })
    .UseCustomConfiguration() // Custom extension method for host configuration
    .Build();

await host.RunAsync();
```

### **2. Service Resolution**

Use `ServiceProviderExtensions` to resolve and manage services dynamically:

```csharp
using Franz.Common.Hosting.Extensions;

var service = host.Services.GetRequiredServiceWithLogging<IMyService>();
service.Execute();
```

### **3. Hosting Initialization**

Implement the `IHostingInitializer` interface for initializing hosting environments:

```csharp
using Franz.Common.Hosting;

public class MyHostingInitializer : IHostingInitializer
{
    public void Initialize(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IMyService, MyService>();
        });
    }
}
```

Register the initializer during application startup:

```csharp
hostBuilder.UseHostingInitializer<MyHostingInitializer>();
```

---

## **Integration with Franz Framework**

The **Franz.Common.Hosting** package integrates seamlessly with:
- **Franz.Common**: Provides foundational utilities.
- **Franz.Common.DependencyInjection**: Simplifies dependency injection setup.
- **Franz.Common.Reflection**: Enables dynamic discovery and registration of services and initializers.

Ensure these dependencies are installed to leverage the full capabilities of the library.

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
- Upgrade version to .net 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**


### Version 1.6.20
- Updated to **.NET 10.0**