# **Franz.Common.SSO**

A library within the **Franz Framework** designed to provide streamlined support for Single Sign-On (SSO) using ASP.NET Core Identity and Entity Framework Core. This package includes interfaces and implementations for managing SSO providers and configuring SSO services.

---

## **Features**

- **SSO Provider Management**:
  - `ISsoProvider` interface for defining custom SSO providers.
  - `GenericSSOProvider` and `GenericSSOManager` for generic implementations of SSO workflows.
- **Service Registration**:
  - `SsoServiceRegistration` to simplify the integration of SSO services into your application.
- **ASP.NET Core Identity Integration**:
  - Full support for ASP.NET Core Identity and Entity Framework Core for identity management.

---

## **Version Information**

- **Current Version**: 1.4.1
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on the following dependencies:
- **Microsoft.AspNetCore.Identity** (2.2.0): Provides core Identity functionality.
- **Microsoft.AspNetCore.Identity.EntityFrameworkCore** (8.0.0): Adds Entity Framework Core integration for ASP.NET Identity.

Additionally, it integrates with:
- **Franz.Common.EntityFramework**: Provides foundational Entity Framework utilities.

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
dotnet add package Franz.Common.SSO  
```

---

## **Usage**

### **1. Register SSO Services**

Use `SsoServiceRegistration` to register SSO services in your application:

```csharp
using Franz.Common.SSO.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSsoServices(options =>
        {
            options.DefaultProvider = "YourSSOProvider";
        });
    }
}
```

### **2. Implement a Custom SSO Provider**

Create a custom implementation of `ISsoProvider`:

```csharp
using Franz.Common.SSO.Interfaces;

public class CustomSSOProvider : ISsoProvider
{
    public Task<string> AuthenticateAsync(string token)
    {
        // Custom authentication logic
        return Task.FromResult("AuthenticatedUserId");
    }
}
```

### **3. Use the GenericSSOManager**

Leverage the `GenericSSOManager` for managing SSO workflows:

```csharp
using Franz.Common.SSO;

public class SsoService
{
    private readonly GenericSSOManager _ssoManager;

    public SsoService(GenericSSOManager ssoManager)
    {
        _ssoManager = ssoManager;
    }

    public async Task<string> AuthenticateUserAsync(string token)
    {
        return await _ssoManager.AuthenticateAsync(token);
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.SSO** package integrates seamlessly with the **Franz Framework**, enabling secure and efficient single sign-on functionality for distributed systems. Use it alongside other Franz packages for enhanced identity and access management.

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
- Added `ISsoProvider` for custom SSO provider implementation.
- Introduced `GenericSSOProvider` and `GenericSSOManager` for generic SSO workflows.
- Integrated with ASP.NET Core Identity and Entity Framework Core.
- Provided `SsoServiceRegistration` for streamlined service configuration.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**