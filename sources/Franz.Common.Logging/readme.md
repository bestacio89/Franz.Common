# **Franz.Common.Logging**

A comprehensive logging library within the **Franz Framework**, designed to enhance application monitoring and diagnostics using **Serilog** and **Elastic APM**. This package provides tools for centralized logging, tracing, and seamless integration with ASP.NET Core applications.

---

## **Features**

- **Centralized Logging**:
  - Integrates with **Serilog** for structured and enriched logging.
- **Tracing Utilities**:
  - `TraceHelper` for advanced tracing support in distributed applications.
- **Host Extensions**:
  - `HostBuilderExtensions` for configuring logging and tracing in application startup.
- **Elastic APM Integration**:
  - Provides support for **Elastic APM** to monitor application performance and detect issues.

---

## **Version Information**

- **Current Version**: 1.4.2
- Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

This package relies on:
- **Elastic.Apm.NetCoreAll** (1.25.1): Full integration with Elastic APM for application performance monitoring.
- **Elastic.Apm.SerilogEnricher** (8.6.1): Enriches Serilog logs with Elastic APM trace data.
- **Serilog.AspNetCore** (8.0.0): Enables Serilog integration in ASP.NET Core applications.
- **Serilog.Enrichers.Demystifier** (1.0.2): Enhances log messages by providing demystified stack traces.
- **Serilog.Enrichers.Environment** (2.3.0): Adds environment-based enrichments to log data.

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
dotnet add package Franz.Common.Logging  
```

---

## **Usage**

### **1. Configure Logging in Host Builder**

Use `HostBuilderExtensions` to configure logging and tracing:

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseLoggingWithElasticApm() // Configures Serilog and Elastic APM
    .Build();

await host.RunAsync();
```

### **2. Add Custom Tracing**

Leverage `TraceHelper` to add custom tracing to your application:

```csharp
using Franz.Common.Logging.Tracing;

TraceHelper.TraceInformation("Starting application initialization...");

// Add custom trace messages as needed
TraceHelper.TraceWarning("Potential configuration issue detected.");
```

### **3. Enrich Log Data**

Take advantage of Serilog enrichers for enhanced log metadata:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithEnvironmentName()
    .Enrich.WithDemystifiedStackTraces()
    .Enrich.WithElasticApmCorrelationInfo()
    .WriteTo.Console()
    .CreateLogger();
```

### **4. Monitor Performance with Elastic APM**

Ensure Elastic APM is set up and logs include performance-related trace data. Add Elastic APM server configurations in `appsettings.json`:

```json
{
  "ElasticApm": {
    "ServerUrls": "http://localhost:8200",
    "ServiceName": "MyApplication"
  }
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Logging** package integrates seamlessly with:
- **Franz Framework**: Acts as the central logging mechanism for all Franz libraries.
- **Elastic APM**: Enables distributed tracing and performance monitoring.
- **Serilog**: Provides structured logging and enrichments for ASP.NET Core applications.

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

Version 1.4.1

Introduced environment-aware logging defaults via UseLog() extension.

Development → Verbose console + rolling file logs (7-day retention).

Production → Concise console + structured JSON rolling logs (30-day retention).

Bound static Serilog.Log.Logger to ensure logs from Polly callbacks and other static calls flow through configured sinks.

Improved Serilog + Elastic APM integration with correlation IDs and structured enrichments.

Added rolling file support with automatic daily log rotation.

Enhanced TraceHelper with support for correlation context propagation.

Version 1.3

Upgraded to .NET 9.0.8

Added new features and improvements

Separated business concepts from mediator concepts

Now compatible with both the in-house mediator and MediatR

Version 1.2.65

Upgrade version to .NET 9