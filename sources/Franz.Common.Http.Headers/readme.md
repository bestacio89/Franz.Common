# **Franz.Common.Http.Headers**

A library within the **Franz Framework** designed to streamline the management, validation, and processing of HTTP headers in **ASP.NET Core** applications. This package provides custom model binders, attributes, and utilities for working with headers, ensuring consistency and efficiency across applications.

---

## **Features**

- **Header Validation**:
  - `HeaderRequiredAttribute` for enforcing required headers on API actions.
  - `HeaderRequiredActionConstraint` for conditional routing based on headers.
- **Custom Model Binding**:
  - `ComplexModelBinder` and `HeaderComplexModelBinderProvider` for advanced header parsing and mapping.
- **HTTP Context Utilities**:
  - `HeaderContextAccessor` for simplified access to HTTP header data.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` for easily integrating header-related services.

---

## **Version Information**

- **Current Version**:  1.3.10
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.AspNetCore.Mvc.Core** (2.2.5): Provides core MVC functionality for headers.
- **Newtonsoft.Json** (13.0.3): Advanced JSON serialization for complex header handling.
- **Franz.Common.Headers**: Core header utilities.
- **Franz.Common.Serialization**: Serialization utilities for advanced header operations.

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
dotnet add package Franz.Common.Http.Headers  
```

---

## **Usage**

### **1. Enforce Required Headers**

Use `HeaderRequiredAttribute` to enforce the presence of specific headers:

```csharp
using Franz.Common.Http.Headers.Extensions;

[HttpGet]
[HeaderRequired("X-Custom-Header")]
public IActionResult Get()
{
    return Ok("Header is present");
}
```

### **2. Advanced Header Parsing**

Leverage `ComplexModelBinder` to parse and bind headers to complex objects:

```csharp
using Franz.Common.Http.Headers.Extensions;

public class CustomHeaderModel
{
    public string Key { get; set; }
    public string Value { get; set; }
}

[HttpPost]
public IActionResult Post([ModelBinder(typeof(ComplexModelBinder))] CustomHeaderModel headerModel)
{
    return Ok(headerModel);
}
```

### **3. HTTP Context Access**

Simplify access to header data using `HeaderContextAccessor`:

```csharp
using Franz.Common.Http.Headers.Extensions;

var customHeader = contextAccessor.GetHeaderValue("X-Custom-Header");
```

### **4. Register Header Utilities**

Use `ServiceCollectionExtensions` to register all header-related utilities:

```csharp
using Franz.Common.Http.Headers.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHeaderUtilities(); // Registers header-related services and utilities
    }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Headers** package integrates seamlessly with:
- **Franz.Common.Headers**: Core header utilities and extensions.
- **Franz.Common.Serialization**: Enhances serialization for complex header processing.

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
---

