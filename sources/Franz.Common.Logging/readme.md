# **Franz.Common.Logging**

A comprehensive logging library within the **Franz Framework**, designed to enhance application monitoring and diagnostics using **Serilog** and **Elastic APM**.
This package provides tools for centralized logging, tracing, and seamless integration with ASP.NET Core applications.

* **Current Version**: 1.7.6
* Part of the private **Franz Framework** ecosystem.

---

## **What’s New in 1.7.6**

* **Dual Production Logging**

  * **SRE logs:** JSON, structured, minimal noise, easy to ship to ELK/Datadog.
  * **Dev logs:** human-readable, verbose, retains stack traces for troubleshooting.

* **Self-contained, production-ready defaults**

  * Automatic UTF-8-safe encoding, rolling files, environment-aware enrichers.
  * Global noise suppression applied consistently.

* **Integration-ready for Franz.Common.OpenTelemetry**

  * Logs are automatically correlated with tracing for distributed systems.

* **Fail-safe and maintainable**

  * Dev and SRE logs separate, no interference, retention policies enforced.

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

* **Dual Production Logging (v1.7.6)**

  * JSON logs for SRE (prod-sre-.json)
  * Human-readable logs for Dev (prod-dev-.log)

---

## **Installation**

### From Private Azure Feed

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text
```

Install the package:

```bash
dotnet add package Franz.Common.Logging --version 1.7.6
```

---

## **Usage**

### 1. Strict Environment-Aware Logging (`UseLog`)

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseLog() // Dev → Console + Debug + File
              // Prod → Console + Dev log + SRE log
    .Build();

await host.RunAsync();
```

**Development:**

* Verbose console logs
* Debug sink
* Daily rolling file logs (`logs/dev-.log`)

**Production:**

* **SRE JSON log:** `logs/prod-sre-.json`, structured, minimal noise, 30-day retention
* **Dev human-readable log:** `logs/prod-dev-.log`, verbose, stack traces, 30-day retention
* Console logs for live monitoring

---

### 2. Hybrid Logging (`UseHybridLog`)

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseHybridLog() // Configuration-driven logging from appsettings.json
    .Build();

await host.RunAsync();
```

**In `appsettings.json`:**

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

```csharp
#if DEBUG
Agent.Setup(new AgentComponents());
#endif
```

**Production:** opt-in via `appsettings.json`:

```json
{
  "ElasticApm": {
    "ServerUrls": "http://localhost:8200",
    "ServiceName": "MyApplication"
  }
}
```

---

## **Changelog**

### Version 1.7.6

* Dual production logs: `prod-sre-.json` for SRE, `prod-dev-.log` for Dev
* UTF-8 safe, rolling files, 30-day retention
* Console logging preserved for live monitoring
* Noise suppression applied consistently across logs
* Integration-ready for **Franz.Common.OpenTelemetry**
* Maintains full backward compatibility with `UseLog()` and `UseHybridLog()`

### Version 1.6.20

* Updated to **.NET 10.0**
* Logging overhaul, platform stability, noise filtering, UTF-8 enforcement, context enrichment

(Older versions omitted for brevity)

---

### Suggested Commit Message

```
chore(logging): release Franz.Common.Logging 1.7.6

- Dual production logging: SRE JSON + Dev human-readable logs
- UTF-8 safe, rolling files, 30-day retention
- Preserves console logging for live monitoring
- Noise suppression applied consistently
- Ready for integration with Franz.Common.OpenTelemetry
- Updated README and usage examples
```

