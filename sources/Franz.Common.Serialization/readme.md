# Franz.Common.Serialization

> Shared framework capability for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.Serialization` package provides shared framework capability within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.Serialization
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

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

**Current Version:** v2.3.3
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

### Version 1.6.20
- Updated to **.NET 10.0**


### Version 1.7.4
- Minor bug fixes and performance improvements
- Updated dependencies to latest versions of System.Text.Json

---

## Dependency maintenance — October 2026

This project's dependency references were reviewed and updated as part of the repository-wide .NET 10 maintenance pass. The intended target framework and public API remain unchanged. Verify the resolved direct and transitive dependency graph through the repository's dependency validation workflow before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
