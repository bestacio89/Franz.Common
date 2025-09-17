# **Franz.Common.Annotations**

A lightweight library within the **Franz Framework** designed to provide custom attribute-based annotations for simplifying Kafka topic validation in .NET applications.

---

## **Features**

- **Custom Kafka Topic Annotation**:
  - `RequiredKafkaTopicAttribute`: Ensures that a Kafka topic is specified and validated for producer/consumer configurations.

---

## **Version Information**

- **Current Version**:  1.3.8
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
dotnet add package Franz.Common.Annotations  
```

---

## **Usage**

### **1. Apply `RequiredKafkaTopicAttribute`**

Use the `RequiredKafkaTopicAttribute` to annotate properties or fields that represent Kafka topics in your application. This ensures that all required topics are explicitly defined and validated at runtime.

Example:

```csharp
using Franz.Common.Annotations;

public class KafkaConfiguration
{
    [RequiredKafkaTopic]
    public string OrdersTopic { get; set; }

    [RequiredKafkaTopic]
    public string PaymentsTopic { get; set; }
}
```

### **2. Validation**

Ensure that annotated properties are checked during application initialization:

```csharp
var kafkaConfig = new KafkaConfiguration
{
    OrdersTopic = "orders",
    PaymentsTopic = string.Empty // Validation will fail here
};

ValidateKafkaConfiguration(kafkaConfig);

void ValidateKafkaConfiguration(object config)
{
    var properties = config.GetType().GetProperties();

    foreach (var property in properties)
    {
        var attribute = property.GetCustomAttribute<RequiredKafkaTopicAttribute>();
        if (attribute != null && string.IsNullOrWhiteSpace(property.GetValue(config)?.ToString()))
        {
            throw new InvalidOperationException($"The Kafka topic '{property.Name}' is required but was not provided.");
        }
    }
}
```

---

## **Dependencies**

This package has no external dependencies but integrates seamlessly with other **Franz Framework** libraries.

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

