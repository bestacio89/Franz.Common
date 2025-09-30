# **Franz.Common.Http.Authentication**

A specialized library within the **Franz Framework** that provides streamlined configurations for **JWT Bearer Authentication** and Swagger integration in **ASP.NET Core** applications. This package simplifies secure API development by integrating authentication mechanisms and enhancing API documentation with authentication workflows.

---

## **Features**

- **JWT Bearer Authentication**:
  - Simplifies configuration and registration of JWT Bearer Authentication for secure APIs.
- **Swagger Integration**:
  - Enhances Swagger documentation with JWT support using `Swashbuckle.AspNetCore.SwaggerGen`.
- **Service Registration**:
  - `ServiceCollectionExtensions` for easy setup of authentication services and Swagger enhancements.

---

## **Version Information**

- ** Current Version**: 1.6.0
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Authentication.JwtBearer** (8.0.0): Provides middleware for JWT authentication in ASP.NET Core.
- **Swashbuckle.AspNetCore.SwaggerGen** (6.5.0): Adds Swagger generation capabilities for APIs with authentication.
- **Microsoft.NETCore.App**: Core framework for .NET applications.

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
dotnet add package Franz.Common.Http.Authentication  
```

---

## **Usage**

### **1. Configuring JWT Bearer Authentication**

Use the `ServiceCollectionExtensions` to configure JWT authentication:

```csharp
using Franz.Common.Http.Authentication.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddJwtBearerAuthentication(options =>
        {
            options.Authority = "https://your-auth-server";
            options.Audience = "your-api-audience";
        });
    }
}
```

This automatically registers the necessary middleware for JWT authentication.

### **2. Swagger Integration with Authentication**

Enable JWT authentication in your Swagger configuration:

```csharp
using Franz.Common.Http.Authentication.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerWithJwtSupport(); // Adds Swagger integration with JWT
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        });
    }
}
```

### **3. Protecting API Endpoints**

Secure your endpoints with `[Authorize]` attributes:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetOrders()
    {
        return Ok(new[] { "Order1", "Order2" });
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Authentication** package integrates seamlessly with:
- **Franz.Common.Http**: Provides complementary HTTP utilities.
- **Franz.Common**: Core utilities for shared functionality.

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


