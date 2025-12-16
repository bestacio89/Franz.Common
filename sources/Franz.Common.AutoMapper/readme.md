# **Franz.Common.AutoMapper**

A utility library within the **Franz Framework** designed to streamline the integration and configuration of **AutoMapper** in .NET applications. This package includes helpful extensions for dependency injection, simplifying the setup and usage of AutoMapper profiles.

---

## **Features**

- **AutoMapper Dependency Injection**:
  - `ServiceCollectionExtensions` to easily register AutoMapper with all your profiles in a single call.
- **Simplified AutoMapper Configuration**:
  - Automatically scan and load mapping profiles for your application.

---

## **Version Information**

- **Current Version**: 1.7.0
- Part of the private **Franz Framework** ecosystem.

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
dotnet add package Franz.Common.AutoMapper  
```

---

## **Usage**

### **1. Register AutoMapper with Dependency Injection**

Use the `ServiceCollectionExtensions` to register all your mapping profiles automatically:

```csharp
using Franz.Common.AutoMapper;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAutoMapperProfiles(); // Automatically loads all profiles
    }
}
```

### **2. Create AutoMapper Profiles**

Define your mapping profiles as usual:

```csharp
using AutoMapper;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderDto, Order>();
    }
}
```

The `AddAutoMapperProfiles` method will scan and register all classes inheriting from `Profile`.

---

## **Dependencies**

This package depends on:
- **AutoMapper** (>= 12.0.0)

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
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**
---

### Version 1.6.20
- Updated to **.NET 10.0**