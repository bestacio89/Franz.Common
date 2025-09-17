# **Franz.Common.Http.Client**

A powerful library within the **Franz Framework** that simplifies the creation, configuration, and management of HTTP clients in .NET applications. This package provides tools for handling HTTP requests, authentication, custom request builders, and error management, enabling consistent and efficient communication with external APIs.

---

## **Features**

- **HTTP Client Abstractions**:
  - `HttpService` for streamlined HTTP request execution.
  - `HttpClientException` for managing HTTP-related errors.
- **Authentication**:
  - `AuthenticationService` for token-based authentication workflows.
- **Request Builders**:
  - Customizable request builders for advanced HTTP scenarios:
    - `AuthorizationRequestBuilder`
    - `DomainRequestBuilder`
    - `HeaderPropagationRequestBuilder`
- **Delegating Handlers**:
  - Middleware for HTTP client pipelines:
    - `ExceptionDelegatingHandler`
    - `RequestBuilderDelegatingHandler`
- **File Handling**:
  - `HttpFileParameter` and `FileParameter` for file uploads.
- **Dependency Injection**:
  - `ServiceCollectionExtensions` to register HTTP client services easily.
- **Content Utilities**:
  - `HttpContentParameter` for building HTTP content with flexibility.

---

## **Version Information**

- **Current Version**:  1.3.7
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Microsoft.Extensions.Http** (8.0.0): Provides core HTTP client factory functionality.
- **Newtonsoft.Json** (13.0.3): Advanced JSON serialization and deserialization.
- **Microsoft.Extensions.Configuration.Abstractions** (8.0.0): Configuration management utilities.
- **Franz.Common**: Core utilities for shared functionality.
- **Franz.Common.DependencyInjection**: Simplified DI setup.
- **Franz.Common.Headers**: HTTP header utilities and extensions.
- **Franz.Common.Errors**: Error handling and management.
- **Franz.Common.Identity**: Identity integration for authentication.
- **Franz.Common.MultiTenancy**: Multi-tenant HTTP client configurations.
- **Franz.Common.Serialization**: Serialization utilities for API communication.

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
dotnet add package Franz.Common.Http.Client  
```

---

## **Usage**

### **1. Configure HTTP Client Services**

Use `ServiceCollectionExtensions` to register HTTP clients:

```csharp
using Franz.Common.Http.Client.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClientServices(); // Registers HTTP clients and related services
    }
}
```

### **2. Execute HTTP Requests**

Use the `HttpService` for simplified HTTP interactions:

```csharp
using Franz.Common.Http.Client.Files;

var httpService = serviceProvider.GetRequiredService<HttpService>();

var response = await httpService.GetAsync<MyResponse>("https://api.example.com/resource");
```

### **3. Custom Request Builders**

Create custom request configurations with request builders:

```csharp
using Franz.Common.Http.Client.Delegating;

var requestBuilder = new DomainRequestBuilder("https://api.example.com")
    .AddHeader("Authorization", "Bearer token")
    .AddQueryParameter("key", "value");
```

### **4. File Uploads**

Handle file uploads using `HttpFileParameter`:

```csharp
using Franz.Common.Http.Client.Content;

var fileParameter = new HttpFileParameter
{
    FileName = "document.pdf",
    ContentType = "application/pdf",
    Content = fileStream
};

await httpService.PostFileAsync("https://api.example.com/upload", fileParameter);
```

### **5. Authentication**

Integrate authentication workflows with `AuthenticationService`:

```csharp
using Franz.Common.Http.Client.Authentication;

var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
var token = await authService.GetAccessTokenAsync("my-client-id", "my-client-secret");
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Client** package integrates seamlessly with:
- **Franz.Common**: Provides core utilities.
- **Franz.Common.Headers**: Simplifies header management.
- **Franz.Common.Errors**: Handles HTTP-specific errors consistently.
- **Franz.Common.Serialization**: Enhances serialization for HTTP payloads.
- **Franz.Common.MultiTenancy**: Enables tenant-specific HTTP configurations.

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