# **Franz.Common.Http.Identity**

A utility library within the **Franz Framework** that enhances **ASP.NET Core** identity management for HTTP-based applications. This package provides tools for accessing and managing identity context in a consistent and efficient manner.

---

## **Features**

- **Identity Context Access**:
  - `IdentityContextAccessor` for simplified access to the current user's identity information in HTTP requests.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for seamless integration of identity-related services.

---

## **Version Information**

- **Current Version**: 1.2.65
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Mvc.Core** (2.2.5): Core MVC functionalities for identity and authentication.
- **Franz.Common.Identity**: Core identity utilities and extensions.
- **Franz.Common.Http**: HTTP utilities and middleware extensions.
- **Franz.Common.Headers**: Header management utilities for HTTP applications.

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
dotnet add package Franz.Common.Http.Identity --Version 1.2.65
```

---

## **Usage**

### **1. Access Identity Context**

Leverage `IdentityContextAccessor` to access the current user's identity information:

```csharp
using Franz.Common.Http.Identity;

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

### **2. Register Identity Utilities**

Use `ServiceCollectionExtensions` to register identity-related services:

```csharp
using Franz.Common.Http.Identity.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIdentityContext(); // Registers identity context utilities
    }
}
```

### **3. Use Identity Context in Controllers**

Access identity data directly in controllers:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IdentityContextAccessor _identityContextAccessor;

    public UserController(IdentityContextAccessor identityContextAccessor)
    {
        _identityContextAccessor = identityContextAccessor;
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = _identityContextAccessor.User?.FindFirst("sub")?.Value;
        return Ok(new { UserId = userId });
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Identity** package integrates seamlessly with:
- **Franz.Common.Identity**: Provides foundational identity utilities.
- **Franz.Common.Http**: Enhances HTTP functionality for identity-based applications.
- **Franz.Common.Headers**: Simplifies header-based identity management.

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
