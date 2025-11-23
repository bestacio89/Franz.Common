# Authentication Model – Franz.Common

Franz.Common does not enforce a specific identity provider.  
Instead, it provides integration points for standard authentication mechanisms:

- OAuth2 / OpenID Connect (recommended),
- JWT bearer authentication,
- Cookie-based auth for legacy/internal systems.

Typical setup:

- ASP.NET Core authentication middleware is configured with:
  - authority (e.g., Azure AD, Keycloak, ADFS),
  - audience (API identifiers),
  - scopes/roles (optional).
- Franz.Http:
  - ensures correlation IDs are propagated,
  - exposes helpers for extracting user identity and claims,
  - supports attaching user context to logs/audit events.

Authentication is configured at the **host application** level.  
Franz modules assume a valid `ClaimsPrincipal` is available in the pipeline.
