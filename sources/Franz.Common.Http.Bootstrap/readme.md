# **Franz.Common.Http.Bootstrap**

A comprehensive library within the **Franz Framework**, designed to centralize and simplify the configuration of **ASP.NET Core** applications. This package provides a unified setup for HTTP-related features, such as authentication, headers, documentation, and multi-tenancy, by leveraging other **Franz Framework** components.

---

## **Features**

- **Centralized Bootstrap**:
  - Combines HTTP-related functionality into a single streamlined configuration process.
- **Integrated Extensions**:
  - `ApplicationBuilderExtensions` and `HostBuilderExtensions` for pipeline and hosting customization.
  - `ServiceCollectionExtensions` for dependency injection setup.
- **Modular Integration**:
  - Seamlessly integrates with:
    - `Franz.Common.Http`
    - `Franz.Common.Http.Authentication`
    - `Franz.Common.Http.Headers`
    - `Franz.Common.Http.Documentation`
    - `Franz.Common.Http.Identity`
    - `Franz.Common.Http.MultiTenancy`

---

## **Version Information**

- **Current Version**:  1.3.8
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package depends on the following **Franz Framework** components:
- **Franz.Common.Bootstrap**: For application initialization and modular configuration.
- **Franz.Common.Http**: Core HTTP utilities.
- **Franz.Common.Http.Authentication**: Simplified JWT authentication setup.
- **Franz.Common.Http.Headers**: HTTP header utilities and extensions.
- **Franz.Common.Http.Documentation**: API documentation utilities (e.g., Swagger).
- **Franz.Common.Http.Identity**: Identity management for HTTP-based applications.
- **Franz.Common.Http.MultiTenancy**: Multi-tenant configurations for HTTP services.

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
dotnet add package Franz.Common.Http.Bootstrap  
```

---

## **Usage**

### **1. Unified HTTP Bootstrap**

Use the `ServiceCollectionExtensions` to bootstrap HTTP-related functionality in one step:

```csharp
using Franz.Common.Http.Bootstrap.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpBootstrap(); // Registers all HTTP-related services
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpBootstrap(); // Configures middleware pipeline
    }
}
```

### **2. Customizing Host Configuration**

Extend `HostBuilder` to customize hosting initialization:

```csharp
using Franz.Common.Http.Bootstrap.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseHttpBootstrap() // Adds Franz HTTP hosting configuration
    .Build();

await host.RunAsync();
```

### **3. Modular Integration**

Use this package to integrate functionality from other HTTP-related **Franz Framework** libraries:
- **Authentication**: Easily set up JWT authentication.
- **Headers**: Manage custom headers.
- **Documentation**: Enable Swagger for API documentation.
- **MultiTenancy**: Support tenant-specific HTTP configurations.

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Bootstrap** package acts as a central hub for integrating and configuring HTTP-related components. It relies on and integrates seamlessly with:
- **Franz.Common.Bootstrap**
- **Franz.Common.Http**
- **Franz.Common.Http.Authentication**
- **Franz.Common.Http.Headers**
- **Franz.Common.Http.Documentation**
- **Franz.Common.Http.Identity**
- **Franz.Common.Http.MultiTenancy**

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

### Version 1.3.4
- Eliminated dependency on `Franz.Common.Bootstrap` to Automapper

### Version 1.3.6
- Compatible with Franz 1.3.6 stack.
- Self-contained middleware bootstrap (UseHttpArchitecture).
- Swagger & pipeline setup hidden behind Franz extensions.