# **Franz.Common.Http.Documentation**

A robust library within the **Franz Framework** designed to simplify and enhance API documentation for **ASP.NET Core** applications. This package provides seamless integration with Swagger, API versioning, and route conventions, enabling clear, versioned, and comprehensive API documentation.

---

## **Features**

- **Swagger Integration**:
  - Utilities for configuring and customizing Swagger UI and generation options.
- **API Versioning**:
  - Built-in support for API versioning through `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer`.
- **Route Customization**:
  - `RoutePrefixConvention` for consistent routing across controllers.
- **Extensions**:
  - Tools to configure Swagger, MVC options, and application middleware pipelines.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for easy integration of Swagger and API documentation services.

---

## **Version Information**

- **Current Version**:  1.3.10
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer** (5.1.0): Enables API versioning and metadata discovery.
- **Swashbuckle.AspNetCore.SwaggerGen** (6.5.0): For Swagger document generation.
- **Swashbuckle.AspNetCore.SwaggerUI** (6.5.0): For rendering Swagger UI.
- **Microsoft.Extensions.DependencyInjection.Abstractions** (8.0.0): Provides DI abstractions.
- **Franz.Common.Business**: Provides core business utilities.
- **Franz.Common.Reflection**: Reflection utilities for dynamic configuration and customization.

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
dotnet add package Franz.Common.Http.Documentation  
```

---

## **Usage**

### **1. Configure Swagger**

Use the provided `ConfigureSwaggerOptions` class to customize Swagger settings:

```csharp
using Franz.Common.Http.Documentation.Configuration;

services.AddSwaggerGen(options =>
{
    ConfigureSwaggerOptions.Configure(options, apiVersionDescriptionProvider);
});
```

### **2. API Versioning**

Enable API versioning with minimal effort:

```csharp
services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});
services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
});
```

### **3. Swagger and Swagger UI Integration**

Use `ServiceCollectionExtensions` and `ApplicationBuilderExtensions` to integrate Swagger:

```csharp
using Franz.Common.Http.Documentation.Extensions;

services.AddSwaggerDocumentation();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});
```

### **4. Route Prefix Customization**

Customize API routes with `RoutePrefixConvention`:

```csharp
using Franz.Common.Http.Documentation.Routing;

services.AddMvc(options =>
{
    options.Conventions.Add(new RoutePrefixConvention("api"));
});
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Documentation** package integrates seamlessly with:
- **Franz.Common.Business**: Provides business logic utilities.
- **Franz.Common.Reflection**: Enhances reflection capabilities for dynamic configurations.

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