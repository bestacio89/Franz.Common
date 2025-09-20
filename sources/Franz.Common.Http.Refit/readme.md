# Franz.Common.Http.Refit

**Package**: `Franz.Common.Http.Refit`
**Version**: 1.4.1

Refit integration for the Franz Framework — production-oriented, small-surface, high-value.
Provides typed Refit clients pre-wired with: correlation & tenant header propagation, optional token injection, Polly policy integration, Serilog-friendly logging, and OpenTelemetry-friendly annotations & lightweight metrics.

---

## Goals

* Make outbound HTTP clients trivial and consistent across services.
* Reuse Franz primitives (eg. `MediatorContext` for correlation/tenant data and your shared Polly registry).
* Keep the API small and predictable while shipping production ergonomics by default.

---

## Features

* `AddFranzRefit<TClient>(...)` — single-line registration for typed Refit clients.
* Automatic injection of `X-Correlation-ID`, `X-Tenant-Id`, and optional `X-User-Id` headers via `FranzRefitHeadersHandler`.
* Optional `FranzRefitAuthHandler` driven by a pluggable `ITokenProvider` for Bearer tokens.
* Optional Polly policy attachment using the host `IPolicyRegistry<string>` (via `AddPolicyHandlerFromRegistry`).
* Activity enrichment: annotates `Activity.Current` with `franz.http.*` tags (host controls exporters).
* Lightweight metrics via `System.Diagnostics.Metrics` (meter name `Franz.Refit`): request count, failures, duration histogram.
* Small test surface (header handler unit-tested pattern included).

---

## Quickstart

### Add package

```bash
dotnet add package Franz.Common.Http.Refit --version 1.4.1
# Ensure host references Refit.HttpClientFactory and Polly packages (see Dependencies)
```

### Example typed client

```csharp
using Refit;
public interface IWeatherApi
{
  [Get("/weather/today/{city}")]
  Task<WeatherDto> GetTodayAsync(string city);
}

public record WeatherDto(string City, int TemperatureCelsius, string Summary);
```

### Manual registration (code)

```csharp
// register a shared policy registry first (if you want policies)
builder.Services.AddPolicyRegistry()
  .Add("DefaultHttpRetry", HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(300) }));

// register Refit client wired by Franz
builder.Services.AddFranzRefit<MyApp.ApiClients.IWeatherApi>(
    name: "Weather",
    baseUrl: "https://api.weather.local",
    policyName: "DefaultHttpRetry");
```

### Configuration-driven registration (via Franz.Common.Http.Bootstrap)

If `Franz.Common.Http.Bootstrap` is used and `Franz:HttpClients:EnableRefit = true`, the bootstrapper will register clients defined under `Franz:HttpClients:Apis` automatically (see bootstrap README for schema).

---

## API surface (key types)

* `AddFranzRefit<TClient>(IServiceCollection services, string name, string baseUrl, string? policyName = null, Action<RefitSettings>? configureRefitSettings = null, Action<RefitClientOptions>? configureOptions = null)`
  Registers a typed Refit client with header and auth handlers and optional policy.

* `FranzRefitHeadersHandler : DelegatingHandler`
  Injects correlation/tenant/user headers, logs a basic request/response entry, annotates `Activity.Current`, and records metrics.

* `FranzRefitAuthHandler : DelegatingHandler`
  Pluggable token injection. Uses `ITokenProvider` if registered; otherwise a no-op provider is used.

* `RefitClientOptions`
  Per-package options: default timeout, enable OTEL tagging, default policy name, etc.

---

## appsettings example (for bootstrapper usage)

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

* `InterfaceType` (assembly-qualified) is recommended for deterministic resolution.
* `Policy` is optional — if provided, Franz will attach the named policy from the host's policy registry.

---

## Dependencies & recommended versions (NET 9.0.8 compatible)

The host project should reference (or the package may include where appropriate):

```xml
<PackageReference Include="Refit.HttpClientFactory" Version="8.0.0" />
<PackageReference Include="Refit" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.9" />
<PackageReference Include="Polly" Version="8.6.3" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="9.0.9" />
<PackageReference Include="Serilog" Version="4.3.0" />
<PackageReference Include="OpenTelemetry.Api" Version="1.12.0" />
```

> Lock versions per your mono-repo policy. `Refit.HttpClientFactory` provides `AddRefitClient<T>()` — ensure it is referenced in the **project that calls the registration**.

---

## Testing

* Unit test skeleton included for `FranzRefitHeadersHandler` (validates header injection).
* Suggested tests:

  * Header handler adds `X-Correlation-ID` and `X-Tenant-Id`.
  * Auth handler uses `ITokenProvider` when present.
  * Refit client registration attaches Polly policies when the registry contains them (integration test with `TestServer`/`IHttpClientFactory`).

Example unit test pattern:

```csharp
[Fact]
public async Task SendAsync_AddsCorrelationAndTenantHeaders()
{
  MediatorContext.Reset();
  MediatorContext.Current.TenantId = "tenant-42";
  MediatorContext.Current.CorrelationId = "corr-123";

  var inner = new TestHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
  var handler = new FranzRefitHeadersHandler(new NullLogger<FranzRefitHeadersHandler>()) { InnerHandler = inner };

  var invoker = new HttpMessageInvoker(handler);
  var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://dummy/test"), CancellationToken.None);

  Assert.True(inner.LastRequest.Headers.Contains("X-Correlation-ID"));
  Assert.True(inner.LastRequest.Headers.Contains("X-Tenant-Id"));
}
```

---

## Troubleshooting

* **`AddRefitClient<T>()` not found**

  * Ensure `Refit.HttpClientFactory` (or an equivalent Refit package that exposes factory helpers) is referenced in the **same project** that performs the registration. Add `using Refit;` at the top of registration code.

* **Refit clients not registered via bootstrapper**

  * Ensure `Franz:HttpClients:EnableRefit` is `true` and host project references `Franz.Common.Http.Refit`. Provide `InterfaceType` in config if automatic discovery fails.

* **Polly policy not applied**

  * Register your policies in the `IPolicyRegistry<string>` prior to calling `AddFranzRefit` (example: `services.AddPolicyRegistry().Add("DefaultHttpRetry", policy)`).

* **Token injection missing**

  * Register an `ITokenProvider` that returns tokens via `GetTokenAsync`. If none is registered, the auth handler no-ops.

---

## Changelog (recent)

* **v1.4.1**

  * New: `AddFranzRefit<TClient>()` extension for typed Refit clients.
  * New: `FranzRefitHeadersHandler` — correlation/tenant headers, logging, OTEL tags, metrics.
  * New: Optional `ITokenProvider` + `FranzRefitAuthHandler`.
  * New: Polly policy reuse via host `IPolicyRegistry<string>`.

---

## Publishing & Release Checklist

* Bump package version to `1.4.1`.
* Build & run unit tests.
* `dotnet pack -c Release` → produce `.nupkg`.
* `dotnet nuget push ./bin/Release/*.nupkg -k $NUGET_API_KEY -s <feed>` (or use your private feed).
* Update top-level Franz changelog & bootstrap README to mention Refit integration.

---

## Contributing & License

Part of the private Franz Framework. Follow internal contribution guidelines.
Licensed under the MIT License.

---

If you want, I can also:

* Create the ready-to-commit file contents for `Franz.Common.Http.Refit/README.md` (one-shot), or
* Generate the `Franz.Common.Http.Refit` project scaffold (files from our earlier spec) so you can drop it into the repo. Which next?
