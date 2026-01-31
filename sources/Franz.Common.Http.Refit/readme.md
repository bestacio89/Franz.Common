# **Franz.Common.Http.Refit**

**Package**: `Franz.Common.Http.Refit`

Refit integration for the **Franz Framework** — production-oriented, small-surface, and highly modular.
Provides **typed Refit clients** pre-wired with **correlation and tenant propagation**, **optional authentication**, **resilience with Polly**, **Serilog-friendly logging**, and **OpenTelemetry-friendly instrumentation**.

---

* **Current Version**: 1.7.7
- Part of the private **Franz Framework** ecosystem.
---

## **Goals**

* Simplify and standardize outbound HTTP client creation across Franz-based applications.
* Leverage Franz primitives (`MediatorContext`, correlation IDs, and shared Polly registry).
* Ensure all external calls are **traceable, resilient, and predictable** out of the box.
* Maintain a **minimal public API surface** while delivering production-grade ergonomics.

---

## **Features**

* **Unified Client Registration**
  `AddFranzRefit<TClient>(...)` — single-line registration that configures:

  * Base URL
  * Correlation & tenant headers
  * Authentication (optional)
  * Polly resilience policy
  * OpenTelemetry enrichment

* **Header Propagation**
  `FranzRefitHeadersHandler` automatically injects:

  * `X-Correlation-ID`
  * `X-Tenant-Id`
  * `X-User-Id` (if available)

* **Authentication Handler**
  `FranzRefitAuthHandler` integrates via a pluggable `ITokenProvider`.
  Automatically **disables itself** if no provider or options are configured.

* **Resilience Integration**
  Seamless `Polly` policy attachment via `AddPolicyHandlerFromRegistry`.

* **Telemetry and Metrics**

  * Annotates `Activity.Current` with `franz.http.*` tags for distributed tracing.
  * Lightweight internal `System.Diagnostics.Metrics` (Meter: `Franz.Refit`).

---

## **Dependencies**

* **Refit.HttpClientFactory** (8.2.0) — Refit integration with `IHttpClientFactory`.
* **Microsoft.Extensions.Http.Polly** (8.1.2) — HTTP-level resilience policies.
* **Serilog** (8.0.0) — Structured log correlation.
* **OpenTelemetry.Api** (1.8.1) — Distributed tracing support.
* **Polly** (8.1.2) — Retry, circuit-breaker, and fallback strategies.

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
dotnet add package Franz.Common.Http.Refit
```

---

## **Usage**

### 1. Register a Refit Client

```csharp
using Franz.Common.Http.Refit.Extensions;

builder.Services.AddFranzRefit<IMyExternalApi>(
    name: "MyApi",
    baseUrl: "https://api.example.com",
    policyName: "standard-http-retry",
    configureOptions: opt =>
    {
        opt.EnableOpenTelemetry = true;
        opt.DefaultPolicyName = "standard-http-retry";
        opt.Timeout = TimeSpan.FromSeconds(30);
    });
```

✅ **Automatically configures:**

* Correlation & tenant headers (`FranzRefitHeadersHandler`)
* Authentication (via `FranzRefitAuthHandler`, optional)
* Named Polly policy (from registry)
* OpenTelemetry tagging (if enabled)

---

### 2. Authentication (Optional)

Implement a token provider:

```csharp
using Franz.Common.Http.Refit.Contracts;

public sealed class MyTokenProvider : ITokenProvider
{
    public Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        // Fetch from secure store, cache, or identity service
        return Task.FromResult("example-token");
    }
}
```

Register it in DI:

```csharp
builder.Services.AddSingleton<ITokenProvider, MyTokenProvider>();
```

If no `ITokenProvider` is registered, the handler auto-disables and no `Authorization` header is sent.

---

### 3. Configuration via appsettings.json

```json
{
  "RefitClientOptions": {
    "EnableOpenTelemetry": true,
    "DefaultPolicyName": "standard-http-retry",
    "Timeout": "00:00:30"
  }
}
```

Wire configuration:

```csharp
builder.Services.AddFranzRefit<IMyApi>(
    name: "MyApi",
    baseUrl: builder.Configuration["ExternalApi:BaseUrl"]!,
    configureOptions: opt =>
    {
        builder.Configuration.GetSection("RefitClientOptions").Bind(opt);
    });
```

---

### 4. Example Resilience Policy Registration

```csharp
using Polly;
using Polly.Extensions.Http;

builder.Services.AddPolicyRegistry(registry =>
{
    registry.Add("standard-http-retry", HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));
});
```

---

### 5. Example Typed Client Interface

```csharp
using Refit;
using System.Threading.Tasks;

public interface IBooksApi
{
    [Get("/books")]
    Task<ApiResponse<List<BookDto>>> GetBooksAsync();
}
```

---

### 6. DefaultTokenProvider Example (OAuth2)

```csharp
using Franz.Common.Http.Refit.Handlers;
using Microsoft.Extensions.Options;

builder.Services.Configure<DefaultTokenProviderOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.AddHttpClient(nameof(DefaultTokenProvider));
builder.Services.AddSingleton<ITokenProvider, DefaultTokenProvider>();
```

Example `Auth` configuration:

```json
{
  "Auth": {
    "TokenEndpoint": "https://login.example.com/oauth2/token",
    "ClientId": "my-client",
    "ClientSecret": "my-secret",
    "Scope": "api.read"
  }
}
```

This enables a cached OAuth2 token provider without any custom code.

---

## **appsettings (Bootstrapper)**

If `Franz.Common.Http.Bootstrap` is active and `Franz:HttpClients:EnableRefit = true`,
Refit clients can be registered automatically from configuration:

```json
{
  "Franz": {
    "HttpClients": {
      "EnableRefit": true,
      "Apis": {
        "Books": {
          "InterfaceType": "MyApp.ApiClients.IBooksApi, MyApp",
          "BaseUrl": "https://api.example.com",
          "Policy": "standard-http-retry"
        }
      }
    }
  }
}
```

---

## **Changelog**

### **Franz 1.6.17 — Refit Overhaul & Self-Healing Auth**

🔹 **Highlights**

* **Self-Disabling Authentication Handler**

  * `FranzRefitAuthHandler` now auto-deactivates when no token provider or configuration is present.

* **Unified Refit Registration**

  * Simplified `AddFranzRefit<TClient>()` registration combining Refit setup, Polly, OTEL, and Serilog context propagation.

* **Improved Token Provider Contract**

  * `ITokenProvider` fully async, nullable token support, integrated fallback provider.

* **OpenTelemetry Support**

  * Native tag injection with `franz.http.*` naming convention.

* **Config Binding**

  * Direct JSON binding for `RefitClientOptions` (e.g., `Timeout`, `DefaultPolicyName`).

* **Default Token Provider**

  * New optional `DefaultTokenProvider` with client credentials OAuth2 flow support.

* **Better Sandbox Behavior**

  * Auto-switch to `NoOpTokenProvider` when authentication is not required.

---

## **Integration with Franz Framework**

* **Franz.Common.Mediator** — shares correlation and policy context.
* **Franz.Common.Logging** — consistent logging with correlation IDs.
* **Franz.Common.Http.Client** — same conventions for non-Refit clients.

Together, they provide a **fully coherent HTTP and API integration ecosystem** under Franz.

---

## **Contributing**

This package is internal to the Franz Framework.
If you have repository access:

1. Clone: `https://github.com/bestacio89/Franz.Common/`
2. Branch from `develop`.
3. Submit a PR with changelog updates and semantic version bump.

---

## **License**

Licensed under the **MIT License** (see `LICENSE` file).

---

## **Best Practices**

| Scenario          | Recommendation                                                         |
| ----------------- | ---------------------------------------------------------------------- |
| No Auth / Sandbox | Do not register `ITokenProvider`; auth handler disables automatically. |
| Auth APIs         | Register `ITokenProvider` or use `DefaultTokenProvider`.               |
| Resilient APIs    | Use Polly policies from the global registry.                           |
| Observability     | Enable OpenTelemetry tagging and use Serilog for structured logs.      |
| Configuration     | Prefer JSON-bound `RefitClientOptions` for consistency.                |

---

