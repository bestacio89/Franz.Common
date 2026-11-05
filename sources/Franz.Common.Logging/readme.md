# Franz.Common.Logging

> A comprehensive logging library within the Franz Framework , designed to enhance application monitoring and diagnostics using Serilog and Elastic APM . This package provides tools for centralized logging, tracing, and seamless integration with .NET applications.

## Overview

A comprehensive logging library within the Franz Framework , designed to enhance application monitoring and diagnostics using Serilog and Elastic APM . This package provides tools for centralized logging, tracing, and seamless integration with .NET applications.

## Installation

```bash
dotnet add package Franz.Common.Logging
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

A comprehensive logging library within the **Franz Framework**, designed to enhance application monitoring and diagnostics using **Serilog** and **Elastic APM**.
This package provides tools for centralized logging, tracing, and seamless integration with .NET applications.

* **Current Version:** v2.3.4
* Part of the open-source Franz.Common ecosystem.

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

### Install from NuGet.org

```bash
dotnet add package Franz.Common.Logging
```


```

Install the package:

```bash
dotnet add package Franz.Common.Logging --version 2.3.3

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

---

## Dependency maintenance — October 2026

The logging package is aligned with the .NET 10 servicing baseline. Snappier is pinned to patched version 1.3.1 because the dependency graph previously resolved vulnerable 1.0.0. The public logging contracts are not intentionally changed. Validate restore, Release build, and the full solution tests before publishing. See [the central dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).


## License

Licensed under the MIT License. See the repository [LICENSE](../../LICENSE) for details.
