# **Franz.Common.MongoDB**

A library within the **Franz Framework** designed to streamline the integration of MongoDB into .NET applications. This package provides utilities for configuring MongoDB services and registering them with dependency injection.

---

## **Features**

- **MongoDB Service Registration**:
  - `MongoServiceRegistration` simplifies the configuration and registration of MongoDB clients and databases.
- **Custom Configuration**:
  - Easily integrate custom configurations for MongoDB connections.
- **Dependency Injection Support**:
  - Provides out-of-the-box DI integration for MongoDB services.

---

## **Version Information**

- **Current Version**:  1.3.7
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package has no additional dependencies beyond the .NET runtime and MongoDB client libraries.

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
dotnet add package Franz.Common.MongoDB  
```

---

## **Usage**

### **1. Register MongoDB Services**

Use `MongoServiceRegistration` to add MongoDB services to your dependency injection container:

```csharp
using Franz.Common.MongoDB;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMongoDB(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "MyDatabase";
        });
    }
}
```

### **2. Access MongoDB Services**

Inject the MongoDB client or database into your services:

```csharp
public class MyService
{
    private readonly IMongoDatabase _database;

    public MyService(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task InsertDocumentAsync<T>(T document, string collectionName)
    {
        var collection = _database.GetCollection<T>(collectionName);
        await collection.InsertOneAsync(document);
    }
}
```

### **3. Customize Configurations**

Store custom MongoDB configurations in the `config` folder and extend the registration process as needed.

---

## **Integration with Franz Framework**

The **Franz.Common.MongoDB** package integrates seamlessly with the **Franz Framework**, enabling MongoDB-based storage and operations in distributed systems. Combine it with other Franz libraries for enhanced functionality.

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
- Added `MongoServiceRegistration` for streamlined MongoDB service configuration and registration.
- Introduced support for custom MongoDB configurations.


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**