# **Franz.Common.Logging**

A comprehensive logging library within the **Franz Framework**, designed to enhance application monitoring and diagnostics using **Serilog** and **Elastic APM**.
This package provides tools for centralized logging, tracing, and seamless integration with .NET applications.

* **Current Version:** v2.3.0
* Part of the private **Franz Framework** ecosystem.

---

## **What’s New in 2.2.17**

* **Desktop Application Support**
* `UseDesktopLog()` extension providing thread-aware logging for WPF/Avalonia/MAUI.
* `ThreadIdEnricher` added for high-fidelity concurrency diagnostics.
* Optimized templates: removed web-specific noise (ReqId/Path) to clean up local output.


* **Dual Production Logging (v1.7.6+)**
* **SRE logs:** JSON, structured, minimal noise, easy to ship to ELK/Datadog.
* **Dev logs:** human-readable, verbose, retains stack traces for troubleshooting.


* **Self-contained, production-ready defaults**
* Automatic UTF-8-safe encoding, rolling files, environment-aware enrichers.
* Global noise suppression applied consistently.



---

## **Features**

* **Centralized Logging**
* Integration with **Serilog** for structured and enriched logs.


* **Tracing Utilities**
* `TraceHelper` for advanced tracing support in distributed applications.


* **Host Extensions**
* `UseLog()` for strict, environment-aware logging.
* `UseHybridLog()` for flexible, configuration-driven logging.
* `UseDesktopLog()` for thread-aware desktop diagnostics.


* **Elastic APM Integration**
* Full support for **Elastic APM** (enabled automatically in DEBUG).


* **Dual Production Logging**
* JSON logs for SRE (prod-sre-.json).
* Human-readable logs for Dev (prod-dev-.log).



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
dotnet add package Franz.Common.Logging --version 2.2.18

```

---

## **Usage**

### 1. Strict Environment-Aware Logging (`UseLog`)

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseLog() 
    .Build();

await host.RunAsync();

```

### 2. Desktop-Optimized Logging (`UseDesktopLog`)

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseDesktopLog() 
    .Build();

await host.RunAsync();

```

* **Thread Enrichment:** Injects `ThreadId` for tracking UI vs. background execution.
* **Desktop Template:** `[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | Thread:{ThreadId} | {Message:lj}`.

### 3. Hybrid Logging (`UseHybridLog`)

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseHybridLog() 
    .Build();

await host.RunAsync();

```

---

## **Changelog**

### v2.2.18 - Desktop Diagnostics

* Introduced `UseDesktopLog()` for desktop-first applications.
* Added `ThreadIdEnricher` for high-fidelity concurrency diagnostics.
* Optimized log templates for desktop output, removing web-specific infrastructure noise.

### v2.2.17 - Filter hardening

* Logging filters hardened to prevent accidental log noise in production environments.
* Logging Filters Improved to avoid noise in development environments.

### v2.0.1 – Internal Modernization

* Messaging and infrastructure refactored for async, thread-safety, and modern .NET 10 patterns.
* All APIs remain fully backward compatible.

### Version 1.7.6

* Dual production logs: `prod-sre-.json` for SRE, `prod-dev-.log` for Dev
* UTF-8 safe, rolling files, 30-day retention
* Console logging preserved for live monitoring
* Noise suppression applied consistently across logs
* Integration-ready for **Franz.Common.OpenTelemetry**