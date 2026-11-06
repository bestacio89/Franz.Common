# Franz.Common.Logging

> Serilog host-builder integration with environment-specific sinks, enrichment, and framework-noise filters.

## Installation

```bash
dotnet add package Franz.Common.Logging
```

## Host-builder extensions

The package exposes three extensions in `Franz.Common.Logging.Extensions`.

### UseLog

```csharp
using Franz.Common.Logging.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseLog()
    .Build();

await host.RunAsync();
```

`UseLog()` configures Serilog from host configuration and registered services, enriches events with log context, application name, machine name, and environment name, and applies the package's baseline source filters.

- Development: Debug minimum level, console and debug sinks, and daily rolling `logs/dev-.log` files with a retained-file limit of 10.
- Non-development: Information minimum level, console output, daily JSON `logs/prod-sre-.json` and human-readable `logs/prod-dev-.log` files.

The extension configures UTF-8 console encoding and invokes `TraceHelper.LogConsole()`. Elastic APM agent setup is compiled only in DEBUG builds; it is not an unconditional production APM registration.

### UseDesktopLog

```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseDesktopLog()
    .Build();
```

This uses a desktop-oriented template with thread ID enrichment, console/debug sinks, and daily rolling `logs/desktop-.log` files. It does not add request ID or request path fields to the output template.

### UseHybridLog

```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseHybridLog()
    .Build();
```

This applies baseline filters, reads Serilog configuration, enriches from log context, application name, and machine name. It does not add the explicit console/file sinks used by `UseLog` or `UseDesktopLog`; configure sinks through host Serilog configuration.

## Filtering and enrichment

The baseline configuration excludes selected low-value framework sources, including EF Core internals, HttpClient, Data Protection, static files, host lifetime, health-check diagnostics, Kestrel transport, and selected routing/model-binding/versioning events below Warning. These filters are package defaults; review them against the diagnostic requirements of the deployed application.

## Operational guidance

- Configure sink destinations, retention, access controls, and shipping in accordance with the deployment environment.
- Treat logs as potentially sensitive data. Avoid credentials, tokens, and unnecessary personal data.
- Do not assume the package configures an OpenTelemetry exporter or an Elastic APM production agent.
- Choose one host logging extension deliberately; do not stack multiple competing Serilog configurations.

## License

MIT. See [LICENSE](../../LICENSE).
