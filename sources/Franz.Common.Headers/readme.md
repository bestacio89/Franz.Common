Here’s the README file tailored for the **Franz.Common.Headers** package based on its structure and dependencies:

---

# **Franz.Common.Headers**

A utility library within the **Franz Framework** designed to simplify and standardize header management in .NET applications. This package integrates with the dependency injection system to ensure consistent handling of headers across services and middleware.

---

## **Features**

- **Header Management**:
  - Tools and utilities for managing HTTP headers in applications.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline dependency injection setup for header management.
- **Centralized Configuration**:
  - Supports resource-based configurations using `Resources.resx`.
- **Compatibility**:
  - Seamless integration with **Franz.Common** and **Franz.Common.DependencyInjection**.

---

## **Version Information**

- **Current Version**: 1.7.7
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.Primitives** (8.0.0): Utilities for working with string tokens and collections.
- **Franz.Common**: Core utilities for the framework.
- **Franz.Common.DependencyInjection**: Simplified DI patterns and service extensions.

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
dotnet add package Franz.Common.Headers  
```

---

## **Usage**

### **1. Dependency Injection**

Register header-related services using the `ServiceCollectionExtensions`:

```csharp
using Franz.Common.Headers.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHeaderManagement(); // Registers header utilities and related services
    }
}
```

### **2. Access Headers in Middleware or Services**

Use the registered services to access and manage headers consistently:

```csharp
public class MyHeaderService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MyHeaderService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetHeaderValue(string headerName)
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[headerName].ToString();
    }
}
```

### **3. Resource-Based Configurations**

Utilize `Resources.resx` to centralize header key definitions:

```csharp
var customHeaderKey = Properties.Resources.CustomHeaderKey; // Retrieve header key from resources
```

---

## **Integration with Franz Framework**

The **Franz.Common.Headers** package integrates seamlessly with:
- **Franz.Common**: Core utilities for shared functionality.
- **Franz.Common.DependencyInjection**: Simplifies the DI setup for header management services.

Ensure these dependencies are installed to fully utilize the library.

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

- Upgrade version to .net 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.6.20
- Updated to **.NET 10.0**