# **Franz.Common.EntityFramework.SQLServer**

A private utility library for seamless integration of **Entity Framework Core** with **SQL Server**, designed as part of the **Franz Framework** ecosystem. This package is versioned as `1.2.65` and is hosted exclusively on a private Azure NuGet feed.

---

## **Features**

- **SQL Server Integration**:
  - Simplifies configuration and usage of `Microsoft.EntityFrameworkCore.SqlServer`.
- **Dependency Injection**:
  - Provides `ServiceCollectionExtensions` for easy registration of SQL Server services.
- **SSL Enforcement**:
  - Includes the `SslEnforcement` enum to handle SQL Server SSL configuration.
- **Multi-Tenancy Support**:
  - Integrates with `Franz.Common.MultiTenancy` to streamline tenant-based setups.
- **Part of Franz Framework**:
  - Works seamlessly with other `Franz` libraries, including `Franz.Common.EntityFramework` and `Franz.Common.MultiTenancy`.

---

## **Version Information**

- **Current Version**:  1.3.6
- This package and all related `Franz` packages are under active development and maintained privately.

---

## **Installation**

Since this package is hosted on a private Azure NuGet feed, configure your NuGet client to access the feed before installing.

### **Step 1: Add the Private Azure Feed**
Add the private feed to your NuGet configuration by running:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

### **Step 2: Install the Package**
Install the package via the .NET CLI:

```bash
dotnet add package Franz.Common.EntityFramework.SQLServer  
```

---

## **Usage**

### **1. Configure SQL Server for Entity Framework Core**
Use the provided `ServiceCollectionExtensions` to streamline the setup:

```csharp
using Franz.Common.EntityFramework.SQLServer;

public void ConfigureServices(IServiceCollection services)
{
    services.AddSqlServerDatabase("YourConnectionString");
}
```

### **2. Enable Multi-Tenancy**
Easily configure multi-tenancy when using `Franz.Common.MultiTenancy`:

```csharp
services.AddTenantSupport();
```

### **3. Enforce SSL Settings**
Use the `SslEnforcement` enum to configure SSL options for SQL Server connections:

```csharp
using Franz.Common.EntityFramework.SQLServer.Enums;

SslEnforcement sslOption = SslEnforcement.Required;
```

---

## **Dependencies**

This package is built to work with the following Franz Framework packages (all versioned `1.3.3`):
- `Franz.Common.EntityFramework`
- `Franz.Common.MultiTenancy`

Make sure to install them from your private Azure feed as needed.

---

## **Development Note**

This library is part of the **Franz Framework**, a privately developed and maintained suite of libraries. It is currently **not available on NuGet.org** but is hosted on a private Azure feed for internal use and development purposes.

---

## **Contributing**

Contributions are restricted to the internal development team. If you have access to the private repository, follow these steps:
1. Clone the repository. @ https://github.com/bestacio89/Franz.Common/
2. Create a feature branch for your updates.
3. Submit a pull request for review.

---

## **License**

This library is part of a private framework and subject to internal licensing terms. Contact the author for more details.

---

## **Changelog**

### Version 1.2.65
- Upgrade version to .net 9

### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**