# **Franz.Common.Logging**

A comprehensive logging library within the **Franz Framework**, designed to enhance application monitoring and diagnostics using **Serilog** and **Elastic APM**.
This package provides tools for centralized logging, tracing, and seamless integration with ASP.NET Core applications.

---
- **Current Version**: 1.5.7

---


## **Features**

* **Centralized Logging**

  * Integration with **Serilog** for structured and enriched logs.
* **Tracing Utilities**

  * `TraceHelper` for advanced tracing support in distributed applications.
* **Host Extensions**

  * `UseLog()` for strict, environment-aware logging.
  * `UseHybridLog()` for flexible, configuration-driven logging.
* **Elastic APM Integration**

  * Full support for **Elastic APM** (enabled automatically in DEBUG).

---

## **Version Information**


* Part of the private **Franz Framework** ecosystem.

---

## **Dependencies**

* **Elastic.Apm.NetCoreAll** (1.25.1) – APM agent integration.
* **Elastic.Apm.SerilogEnricher** (8.6.1) – Correlates APM traces with logs.
* **Serilog.AspNetCore** (8.0.0) – ASP.NET Core logging provider.
* **Serilog.Enrichers.Demystifier** (1.0.2) – Clean stack traces in logs.
* **Serilog.Enrichers.Environment** (2.3.0) – Environment metadata enrichment.

---

## **Installation**

### From Private Azure Feed

Configure your NuGet client:

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

### 1. Strict Environment-Aware Logging (`UseLog`)

Hardcoded sinks with enforced dev/prod differences:

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseLog() // Dev ? Console + Debug + File
              // Prod ? Console + JSON file + Log file
    .Build();

await host.RunAsync();
```

* **Development**

  * Verbose console logs
  * Debug sink
  * Daily rolling file logs (`logs/dev-.log`)

* **Production**

  * Concise console logs
  * JSON structured logs (`logs/prod-.json`, 30-day retention)
  * Plain text logs (`logs/prod-.log`, 30-day retention)

---

### 2. Hybrid Logging (`UseHybridLog`)

Configuration-driven sinks (from `appsettings.json`), with enrichers always enforced:

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseHybridLog() // Reads sinks from appsettings.json
    .Build();

await host.RunAsync();
```

In `appsettings.json`, configure sinks per environment:

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": { "path": "logs/app-.log", "rollingInterval": "Day" }
      }
    ]
  }
}
```

---

### 3. Custom Tracing

```csharp
using Franz.Common.Logging.Tracing;

TraceHelper.TraceInformation("Starting application initialization...");
TraceHelper.TraceWarning("Potential configuration issue detected.");
```

---

### 4. Elastic APM (Enabled in DEBUG builds)

`Agent.Setup(new AgentComponents())` is automatically registered in DEBUG mode.
To enable Elastic APM in production, configure `ElasticApm` in `appsettings.json`:

```json
{
  "ElasticApm": {
    "ServerUrls": "http://localhost:8200",
    "ServiceName": "MyApplication"
  }
}
```

---

## **Comparison of Logging Modes**

| Mode             | Sinks Defined | Environment Aware | Configurable via JSON | APM Setup  |
| ---------------- | ------------- | ----------------- | --------------------- | ---------- |
| `UseLog()`       | Hardcoded     | ? Yes (Dev/Prod)  | ? No                  | DEBUG only |
| `UseHybridLog()` | appsettings   | ? No              | ? Yes                 | DEBUG only |

---

## **Integration with Franz Framework**

* Acts as the **central logging mechanism** for all Franz libraries.
* Enables **distributed tracing** and **performance monitoring** via Elastic APM.
* Provides **structured, environment-aware logging** through Serilog.

---

## **Contributing**

This package is private to the Franz Framework. Contributions are limited to the internal development team.

If you have access:

1. Clone the repository:
   `https://github.com/bestacio89/Franz.Common/`
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

This library is licensed under the **MIT License**. See the `LICENSE` file for more details.

---

## **Changelog**

### **1.4.4**

* Added `UseHybridLog()` for appsettings-driven configuration.
* Fixed Serilog provider wiring to respect `appsettings.json`.
* Improved console/file/Elastic sink environment awareness.

### **1.4.1**

* Introduced **environment-aware logging defaults** via `UseLog()`.

  * Development ? Verbose console + daily rolling file (7-day retention).
  * Production ? Concise console + structured JSON (30-day retention).
* Bound `Serilog.Log.Logger` for static logging (e.g., Polly callbacks).
* Improved Elastic APM correlation IDs + structured enrichments.
* Added automatic daily log rotation.
* Enhanced `TraceHelper` with correlation context propagation.

### **1.3**

* Upgraded to **.NET 9.0.8**.
* Separated business concepts from mediator concepts.
* Compatible with both in-house **Mediator** and **MediatR**.

### **1.2.65**

* First upgrade to **.NET 9**.

---

## **Best Practices**

* Use `UseLog()` for **strict, environment-aware logging** in most applications (APIs, background services).
* Use `UseHybridLog()` when you need **flexible, configuration-driven logging**, usually in enterprise apps with strong DevOps pipelines.
* Elastic APM in **production** is always **opt-in** — wire it yourself if your infra requires it. Franz only enables APM automatically in **DEBUG** to help with local testing.

---

## **Comparison of Logging Modes**

| Mode             | Sinks Defined | Environment Aware | Configurable via JSON | APM Setup  |
| ---------------- | ------------- | ----------------- | --------------------- | ---------- |
| `UseLog()`       | Hardcoded     | ? Yes (Dev/Prod)  | ? No                  | DEBUG only |
| `UseHybridLog()` | appsettings   | ? No              | ? Yes                 | DEBUG only |

---


