# **Franz.Common.Http.Identity**

A utility library within the **Franz Framework** that enhances **ASP.NET Core** identity management for HTTP-based applications.
This package provides tools for **identity context access**, **dependency injection**, and **plug-and-play SSO providers** (WS-Fed, OIDC, SAML2, Keycloak).

---
-**Current Version**: 1.7.2
- Part of the private **Franz Framework** ecosystem.
---
## **Features**

* **Identity Context Access**

  * `IdentityContextAccessor`: resolves identity consistently from `HttpContext` (headers & claims).
  * Unified into `FranzIdentityContext` (UserId, Email, FullName, Roles, TenantId, DomainId).

* **Dependency Injection**

  * `AddFranzHttpIdentity()`: registers identity accessor and supporting services.

* **Authentication Providers** (config-driven via `appsettings.json`):

  * `AddFranzWsFedIdentity()` – WS-Federation.
  * `AddFranzOidcIdentity()` – OpenID Connect.
  * `AddFranzSaml2Identity()` – SAML2 (via Sustainsys).
  * `AddFranzKeycloakIdentity()` – Keycloak (OIDC + role normalization).

* **Claims Normalization**

  * All providers map claims into the same `FranzIdentityContext`.
  * Ensures consistent user & tenant resolution across IdPs.

---

## **Installation**

```bash
dotnet add package Franz.Common.Http.Identity
```

---

## **Usage**

### 1. Register Core Identity

```csharp
builder.Services.AddFranzHttpIdentity();
```

---

### 2. Add a Provider

Identity providers are registered via DI extensions and configured in **appsettings.json**.

#### 🔹 WS-Federation

```csharp
builder.Services.AddFranzWsFedIdentity(builder.Configuration);
```

**appsettings.json**

```json
"FranzIdentity": {
  "WsFed": {
    "MetadataAddress": "https://login.microsoftonline.com/{tenant}/federationmetadata/2007-06/federationmetadata.xml",
    "Wtrealm": "https://myapp.example.com/"
  }
}
```

---

#### 🔹 OpenID Connect (OIDC)

```csharp
builder.Services.AddFranzOidcIdentity(builder.Configuration);
```

**appsettings.json**

```json
"FranzIdentity": {
  "Oidc": {
    "Authority": "https://login.microsoftonline.com/{tenant}/v2.0",
    "ClientId": "my-client-id",
    "ClientSecret": "super-secret",
    "CallbackPath": "/signin-oidc"
  }
}
```

---

#### 🔹 SAML2

```csharp
builder.Services.AddFranzSaml2Identity(builder.Configuration);
```

**appsettings.json**

```json
"FranzIdentity": {
  "Saml2": {
    "EntityId": "https://myapp.example.com/",
    "IdpMetadata": "https://idp.example.com/metadata",
    "CallbackPath": "/signin-saml"
  }
}
```

---

#### 🔹 Keycloak (via OIDC)

```csharp
builder.Services.AddFranzKeycloakIdentity(builder.Configuration);
```

**appsettings.json**

```json
"FranzIdentity": {
  "Keycloak": {
    "Authority": "https://keycloak.example.com/auth/realms/myrealm",
    "ClientId": "franz-web",
    "ClientSecret": "super-secret",
    "CallbackPath": "/signin-keycloak"
  }
}
```

---

### 3. Access Identity in Your App

```csharp
app.MapGet("/whoami", (IIdentityContextAccessor ctx) =>
{
    var identity = ctx.GetCurrentIdentity();
    return Results.Ok(new
    {
        identity.UserId,
        identity.Email,
        identity.FullName,
        identity.Roles,
        identity.TenantId,
        identity.DomainId
    });
});
```

---

## **Integration with Franz Framework**

The **Franz.Common.Http.Identity** package integrates seamlessly with:

* **Franz.Common.Identity** → core identity contracts and models.
* **Franz.Common.Http** → HTTP utilities and middleware.
* **Franz.Common.Headers** → standardized header handling for identity propagation.

---

## **Contributing**

This package is part of a private framework. Contributions are limited to the internal development team.

1. Clone the repository @ [Franz.Common](https://github.com/bestacio89/Franz.Common/)
2. Create a feature branch.
3. Submit a pull request for review.

---

## **License**

Licensed under the MIT License. See the `LICENSE` file for details.

---

## **Changelog**

### Version 1.6.2

* Added `IdentityContextAccessor` (HttpContext-based).
* Introduced `AddFranzHttpIdentity()` DI extension.
* Added provider extensions:

  * WS-Federation (`AddFranzWsFedIdentity`).
  * OpenID Connect (`AddFranzOidcIdentity`).
  * SAML2 (`AddFranzSaml2Identity`).
  * Keycloak (`AddFranzKeycloakIdentity`).
* Config-driven setup via `appsettings.json`.
* Unified claims mapping into `FranzIdentityContext`.



### Version 1.3

* Upgraded to **.NET 9.0.8**
* Added **new features and improvements**
* Separated **business concepts** from **mediator concepts**
* Now compatible with both the **in-house mediator** and **MediatR**

### Version 1.2.65

* Upgrade version to .NET 9

---
### Version 1.6.20
- Updated to **.NET 10.0**
