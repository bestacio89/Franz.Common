# Franz.Common.SSO

> HTTP, identity, and service integration for the Franz.Common framework on .NET 10.

## Overview

The `Franz.Common.SSO` package provides HTTP, identity, and service integration within Franz.Common. It is designed to compose with the rest of the framework rather than imposing unrelated infrastructure choices on every application.

## Installation

```bash
dotnet add package Franz.Common.SSO
```

## Design principles

- **Explicit composition:** register only the capabilities the application needs.
- **Clear boundaries:** keep domain and application contracts independent from provider-specific infrastructure where the package's role permits it.
- **Predictable behavior:** make configuration, failure modes, and lifecycle ownership visible rather than relying on hidden conventions.
- **Operational readiness:** validate security, resilience, diagnostics, and compatibility for the actual deployment environment.

## Usage and configuration

Follow the package-specific setup, configuration, and code examples below. Only configure external services when this package requires them, and keep secrets outside source control.

## Package-specific guide

---

# **Franz.Common.SSO**

A library within the **Franz Framework** that provides streamlined support for **Single Sign-On (SSO)** in ASP.NET Core applications.
This package unifies configuration and registration of multiple SSO providers into a single, consistent bootstrapping mechanism, while normalizing all claims into a unified **`FranzIdentityContext`**.

---
-**Current Version:** v2.3.3
---

## **Features**

* **Centralized SSO Bootstrapping**

  * One entry point: `AddFranzSsoIdentity(configuration)`
  * Loads provider settings directly from `appsettings.json`
  * Ensures only one interactive provider is active unless explicitly configured

* **Supported Providers**

  * **WS-Federation** (Azure AD classic / ADFS)
  * **SAML2** (via Sustainsys.Saml2)
  * **OpenID Connect (OIDC)**
  * **Keycloak** (via OIDC, with claims transformation)
  * **JWT Bearer** (API token validation for microservices)

* **Claims Normalization**

  * Maps provider-specific claims (Azure AD, Keycloak, SAML attributes, etc.)
  * Produces a unified `FranzIdentityContext` with:

    * `UserId`, `Email`, `FullName`
    * `Roles`
    * `TenantId`, `DomainId`

* **Structured Logging**

  * Bootstrapping and provider activation logged via `ILogger<T>`
  * Clean integration with **Franz.Common.Logging** / Serilog

* **Configuration-Driven**

  * All providers enabled/disabled via config
  * No hard-coded values in code

---

## **Installation**

From your private Azure feed:

```bash
dotnet nuget add source "https://your-private-feed-url" \
  --name "AzurePrivateFeed" \
  --username "YourAzureUsername" \
  --password "YourAzurePassword" \
  --store-password-in-clear-text

dotnet add package Franz.Common.SSO
```

---

## **Usage**

### **1. Configure appsettings.json**

```json
{
  "FranzIdentity": {
    "AllowMultipleInteractiveProviders": false,
    "WsFederation": {
      "Enabled": false,
      "MetadataAddress": "https://login.microsoftonline.com/...",
      "Wtrealm": "https://your-app"
    },
    "Saml2": {
      "Enabled": false,
      "IdpMetadata": "https://idp.example.com/metadata",
      "EntityId": "https://your-app"
    },
    "Oidc": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "Keycloak": {
      "Enabled": false,
      "Authority": "https://keycloak.example.com/realms/yourrealm",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret"
    },
    "Jwt": {
      "Enabled": true,
      "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
      "Audience": "api://your-api"
    }
  }
}
```

---

### **2. Register SSO in Program.cs**

```csharp
using Franz.Common.SSO.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Franz SSO Identity
builder.Services.AddFranzSsoIdentity(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/whoami", (IIdentityContextAccessor accessor) =>
{
    var identity = accessor.GetCurrentIdentity();
    return Results.Json(identity);
});

app.Run();
```

---

### **3. Normalized Identity Usage**

```csharp
var identity = _accessor.GetCurrentIdentity();
Console.WriteLine($"User: {identity.FullName}, Tenant: {identity.TenantId}, Roles: {string.Join(", ", identity.Roles)}");
```

---

## **Integration with Franz Framework**

* Works with **Franz.Common.Identity** for the core identity context.
* Works with **Franz.Common.Http.Identity** for ASP.NET Core providers.
* Centralizes all SSO wiring into one consistent package.

---

## **Changelog**

### Version 1.6.2

* **Complete SSO overhaul**

  * Removed legacy `GenericSSOManager`/`GenericSSOProvider`
  * Introduced `FranzSsoSettings` for unified config binding
  * Added `AddFranzSsoIdentity()` bootstrap extension
  * Integrated **WS-Fed, SAML2, OIDC, Keycloak, JWT Bearer** providers
  * Added claims normalization pipeline to `FranzIdentityContext`
  * Added structured startup logging via `FranzSsoStartupFilter`

### Version 1.3

* Upgraded to **.NET 9.0.8**
* Added new features and improvements
* Separated **business concepts** from **mediator concepts**
* Now compatible with both the in-house mediator and MediatR

### Version 1.2.65

* Added `ISsoProvider` for custom SSO provider implementation
* Introduced `GenericSSOProvider` and `GenericSSOManager` for generic workflows
* Integrated with ASP.NET Core Identity and EF Core
* Provided `SsoServiceRegistration` for streamlined configuration

---

⚡ With v1.6.2, **Franz.Common.SSO** is now a unified, production-ready SSO abstraction for all supported protocols.

### Version 1.6.20
- Updated to **.NET 10.0**

---

## Dependency maintenance — October 2026

The SSO package uses patched cryptography dependencies and a patched Snappier version where required by the resolved dependency graph. ASP.NET Core integration remains on the .NET 10 shared framework. Confirm SAML/OIDC and cryptographic flows with integration tests before release. See [the repository dependency upgrade record](../../Docs/dependency-upgrade-2026-10.md).
