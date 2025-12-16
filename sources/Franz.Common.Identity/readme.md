# **Franz.Common.Identity**

A foundational library within the **Franz Framework** that provides contracts and utilities for managing **identity context** across applications.
This package defines the **core abstractions** for accessing user identity information in a consistent, provider-agnostic way.

---
-**Current Version**: 1.7.0
- Part of the private **Franz Framework** ecosystem.
---
## **Features**

* **Identity Context Management**

  * `FranzIdentityContext`: unified model (UserId, Email, FullName, Roles, TenantId, DomainId).
  * `IIdentityContextAccessor`: contract for resolving identity across frameworks.

* **Testing Support**

  * `FakeIdentityContextAccessor`: simple stub for unit/integration tests.

* **Framework-Agnostic**

  * No dependency on ASP.NET Core or `HttpContext`.
  * ASP.NET-specific implementation lives in [`Franz.Common.Http.Identity`](../Franz.Common.Http.Identity).

---

## **Installation**

```bash
dotnet add package Franz.Common.Identity
```

---

## **Usage**

### Access Identity Context

```csharp
using Franz.Common.Identity;

public class MyService
{
    private readonly IIdentityContextAccessor _identityContext;

    public MyService(IIdentityContextAccessor identityContext)
    {
        _identityContext = identityContext;
    }

    public FranzIdentityContext GetIdentity()
        => _identityContext.GetCurrentIdentity();
}
```

---

## **Integration with Franz Framework**

The **Franz.Common.Identity** package integrates seamlessly with:

* **Franz.Common.Http.Identity**: ASP.NET Core implementation + SSO providers.
* **Franz.Common.Headers**: standardized HTTP header propagation.

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

* Introduced **`FranzIdentityContext`** (UserId, Email, FullName, Roles, TenantId, DomainId).
* Added **`IIdentityContextAccessor`** interface for framework-agnostic identity access.
* Added **`FakeIdentityContextAccessor`** for testing.
* Moved ASP.NET Core specifics into a new package: **Franz.Common.Http.Identity**, including:

  * `IdentityContextAccessor` (HttpContext-based implementation).
  * `AddFranzHttpIdentity()` DI extension.
  * Provider extensions:

    * `AddFranzWsFedIdentity()` (WS-Federation).
    * `AddFranzOidcIdentity()` (OpenID Connect).
    * `AddFranzSaml2Identity()` (SAML2 via Sustainsys).
    * `AddFranzKeycloakIdentity()` (Keycloak OIDC with role normalization).
* All providers normalize into **`FranzIdentityContext`**.
* Config-driven setup via **appsettings.json** (no hardcoded values).

### Version 1.3

* Upgraded to **.NET 9.0.8**.
* Added new features and improvements.
* Separated business concepts from mediator concepts.
* Now compatible with both in-house mediator and MediatR.

### Version 1.2.65

* Upgraded to **.NET 9**.

---

### Version 1.6.20
- Updated to **.NET 10.0**