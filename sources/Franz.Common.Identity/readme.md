# **Franz.Common.Identity**

A foundational library within the **Franz Framework** that provides tools for managing identity context across **ASP.NET Core** applications. This package simplifies access to user identity information, supporting dependency injection for streamlined integration with other components.

---

## **Features**

- **Identity Context Management**:
  - `IdentityContextAccessor` for accessing user identity data in a consistent manner across applications.
- **Dependency Injection**:
  - Seamless integration with dependency injection via the **Franz.Common.DependencyInjection** package.

---

## **Version Information**

- **Current Version**:  1.3.11
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Franz.Common.DependencyInjection**: Provides dependency injection utilities to simplify service registration.

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
dotnet add package Franz.Common.Identity  
```

---

## **Usage**

### **1. Access Identity Context**

The `IdentityContextAccessor` simplifies access to the current user's identity data:

```csharp
using Franz.Common.Identity;

public class MyService
{
    private readonly IdentityContextAccessor _identityContextAccessor;

    public MyService(IdentityContextAccessor identityContextAccessor)
    {
        _identityContextAccessor = identityContextAccessor;
    }

    public string GetUserId()
    {
        return _identityContextAccessor.User?.FindFirst("sub")?.Value;
    }
}
```

### **2. Register Identity Context**

Integrate `IdentityContextAccessor` into your application:

```csharp
using Franz.Common.Identity;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IdentityContextAccessor>(); // Register the accessor
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Identity** package integrates seamlessly with:
- **Franz.Common.DependencyInjection**: Simplifies registration of the identity context in ASP.NET Core applications.

Ensure this dependency is installed to fully utilize the package's capabilities.

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