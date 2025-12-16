# Franz.Common.Http.Bootstrap

**Package**: `Franz.Common.Http.Bootstrap`
**Current Version**: 1.7.0

Opinionated HTTP bootstrapper for the Franz Framework.
Centralizes common ASP.NET Core HTTP wiring (controllers, auth, headers, docs, serialization, health checks, multi-tenancy) and optionally registers config-driven Refit clients so applications get consistent, production-ready defaults with minimal boilerplate.

---

## Why use this package

* One-line HTTP architecture wiring for consistent apps across teams.
* Plugs into Franz primitives (multi-tenancy, header context, identity, docs).
* Config-driven optional features (Refit clients) keep `Program.cs` tidy.
* Reduces boilerplate and enforces best practices (correlation, tenant propagation, policy reuse).

---

## Quickstart

### Install (private feed)

```bash
# Example: add private feed (adjust your feed credentials)
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "<user>" \
  --password "<pass>" \
  --store-password-in-clear-text

dotnet add package Franz.Common.Http.Bootstrap --version 1.4.1
```

### Minimal wiring (Program.cs)

```csharp
using Franz.Common.Http.Bootstrap.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adds controllers, auth, headers, docs, multi-tenancy, serialization, health, etc.
builder.Services.AddHttpArchitecture(builder.Environment, builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();
app.UseHttpArchitecture(); // optional pipeline helper if provided
app.MapControllers();
app.Run();
```

`AddHttpArchitecture(...)` wires the canonical Franz HTTP stack. If Refit is enabled in configuration it will also register Refit clients automatically.

---

## Refit (optional) — what the bootstrapper does

When `Franz:HttpClients:EnableRefit` is `true`, the bootstrapper will:

* Read `Franz:HttpClients:Apis` from configuration.
* Resolve each typed Refit interface (prefer `InterfaceType` — assembly-qualified; falls back to assembly scan).
* Invoke `AddFranzRefit<TClient>(...)` reflectively to register the typed client.
* For each client it wires:

  * Base address
  * `FranzRefitHeadersHandler` (adds `X-Correlation-ID`, `X-Tenant-Id`, optional `X-User-Id`)
  * Optional `FranzRefitAuthHandler` (if an `ITokenProvider` is registered)
  * Optional Polly policy attachment (by name, via the host's policy registry)

### appsettings.json example (Refit)

```json
{
  "Franz": {
    "HttpClients": {
      "EnableRefit": true,
      "Apis": {
        "Weather": {
          "InterfaceType": "MyApp.ApiClients.IWeatherApi, MyApp",
          "BaseUrl": "https://api.weather.local",
          "Policy": "DefaultHttpRetry"
        }
      }
    }
  }
}
```

**Notes**

* `InterfaceType` (assembly-qualified) is recommended for deterministic resolution.
* `Policy` is optional — if present the bootstrapper will attach the named policy from the host's `IPolicyRegistry<string>`.

---

## Features (at a glance)

* Registers controllers, Swagger documentation, and health checks.
* Authentication and identity context wiring.
* Header context utilities and header-based capabilities.
* Multi-tenancy integration via `Franz.Common.Http.MultiTenancy`.
* Optional, config-driven Refit client registration with correlation, auth, Polly, and OTEL-friendly annotations.
* Opinionated, production-friendly defaults to reduce friction.

---

## Host project dependencies (when using Refit)

When enabling Refit you must ensure the host project references the Refit integration and related primitives:

* `Franz.Common.Http.Refit` (the Refit integration package)
* `Refit.HttpClientFactory` (provides `AddRefitClient<T>()`) or an equivalent Refit package exposing factory helpers
* `Microsoft.Extensions.Http`
* `Polly` & `Microsoft.Extensions.Http.Polly` (if you want policy integration)
* `Serilog` (recommended for enriched logging)
* `OpenTelemetry.Api` (optional; used to tag `Activity.Current`)

> The bootstrapper itself depends on other Franz packages (http modules). Make sure those Franz packages are available to the host.

---

## Troubleshooting

* **Refit clients not registered**:

  * Ensure `"Franz:HttpClients:EnableRefit": true` and that `Franz.Common.Http.Refit` is referenced in the host project.
* **Typed interface cannot be resolved**:

  * Add `InterfaceType` with the assembly-qualified type name (e.g. `MyApp.ApiClients.IWeatherApi, MyApp`) to the config entry or ensure the interface is in the assembly passed to `AddHttpArchitecture`.
* **`AddRefitClient<T>()` not found**:

  * Confirm `Refit.HttpClientFactory` (or the Refit package exposing the factory) is referenced in the project that performs the registration, and add `using Refit;` where needed.
* **Polly policy not applied**:

  * Register policies in `IPolicyRegistry<string>` (`services.AddPolicyRegistry().Add("MyPolicy", policy)`).

---

## Example: manual Refit registration (if you prefer code)

```csharp
builder.Services.AddPolicyRegistry()
  .Add("DefaultHttpRetry", HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(300) }));

builder.Services.AddFranzRefit<MyApp.ApiClients.IWeatherApi>(
    name: "Weather",
    baseUrl: "https://api.weather.local",
    policyName: "DefaultHttpRetry");
```

---

## Changelog (recent)

* **v1.4.1**

  * Added optional Refit client registration via the HTTP bootstrapper (config-driven).
  * Refit clients registered by the bootstrapper support correlation header injection, optional token injection, and Polly policy wiring.

For full changelog/history see the repository `CHANGELOG.md`.

---

## Contributing & License

This package is part of the private Franz Framework. Contributions are internal; follow your team’s contribution guidelines. Licensed under MIT.

