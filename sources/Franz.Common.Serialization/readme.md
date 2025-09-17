# **Franz.Common.Serialization**

A serialization utility library within the **Franz Framework** that simplifies JSON and byte array serialization. This package includes custom JSON converters, serializers, and extensions for seamless integration into .NET applications.

---

## **Features**

- **Custom JSON Converters**:
  - `DateTimeJsonConverter` and `DateTimeOffsetJsonConverter` for handling specific datetime formats.
  - `EnumerationJsonConverter` for serializing and deserializing enumerations.
- **Byte Array Serialization**:
  - `ByteArraySerializer` and `IByteArraySerializer` for efficient byte array serialization and deserialization.
- **JSON Serialization**:
  - `JsonSerializer` and `JsonCreationConverter` for custom JSON serialization logic.
- **Service Registration**:
  - `ServiceCollectionExtensions` to streamline serializer setup and dependency injection.

---

## **Version Information**

- - **Current Version**:  1.3.12
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package is lightweight and has no external dependencies beyond the .NET runtime.

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
dotnet add package Franz.Common.Serialization  
```

---

## **Usage**

### **1. Register Serialization Services**

Use `ServiceCollectionExtensions` to register the serializers:

```csharp
using Franz.Common.Serialization.Extensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSerialization();
    }
}
```

### **2. Serialize and Deserialize JSON**

Leverage `JsonSerializer` for JSON operations:

```csharp
using Franz.Common.Serialization;

var jsonSerializer = new JsonSerializer();
var jsonData = jsonSerializer.Serialize(new { Name = "John", Age = 30 });
var person = jsonSerializer.Deserialize<Person>(jsonData);
```

### **3. Use Custom JSON Converters**

Register and use custom converters like `DateTimeJsonConverter`:

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(new DateTimeJsonConverter());

var json = JsonSerializer.Serialize(DateTime.Now, options);
var dateTime = JsonSerializer.Deserialize<DateTime>(json, options);
```

### **4. Byte Array Serialization**

Utilize `ByteArraySerializer` for byte array operations:

```csharp
using Franz.Common.Serialization;

var byteArraySerializer = new ByteArraySerializer();
var data = new byte[] { 1, 2, 3, 4 };
var serialized = byteArraySerializer.Serialize(data);
var deserialized = byteArraySerializer.Deserialize<byte[]>(serialized);
```

---

## **Integration with Franz Framework**

The **Franz.Common.Serialization** package integrates seamlessly with the **Franz Framework**, providing utilities for efficient serialization and deserialization in distributed systems. Combine it with other Franz packages for enhanced functionality.

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
- Added `DateTimeJsonConverter`, `DateTimeOffsetJsonConverter`, and `EnumerationJsonConverter`.
- Introduced `ByteArraySerializer` and `IByteArraySerializer`.
- Enhanced JSON serialization with `JsonSerializer` and `JsonCreationConverter`.
- Provided `ServiceCollectionExtensions` for streamlined DI setup.

---


### Version 1.3
- Upgraded to **.NET 9.0.8**
- Added **new features and improvements**
- Separated **business concepts** from **mediator concepts**
- Now compatible with both the **in-house mediator** and **MediatR**