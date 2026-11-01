# Franz.Common.Http.MultiTenancy

ASP.NET Core tenant resolution and request execution integration for Franz.Common.

## Install

```bash
dotnet add package Franz.Common.Http.MultiTenancy --version 2.3.4
```

## Registration and middleware order

```csharp
services.AddFranzMultiTenancy(options =>
{
    options.RequireTenant = true;
});

app.UseAuthentication();
app.UseFranzMultiTenancy();
app.UseAuthorization();
```

Place tenant middleware after authentication so authenticated claims are available, and before authorization/endpoints so downstream policies and handlers see validated context.

## Resolution is not authorization

Header, host, query-string and JWT claim resolvers are tenant-selection strategies, not proof of membership. Middleware validates tenant lifecycle and creates immutable request context only after resolution succeeds.

For authenticated principals, register an application implementation of ITenantRequestAuthorizer. If no authorizer is registered, or membership is denied, the request is rejected with HTTP 403. A tenant ID in a header or claim alone is not authorization. Anonymous tenant access remains governed by the application's endpoint authorization policy.

The JWT claim resolver ignores unauthenticated principals. Register only resolution strategies appropriate to the application; caller-controlled headers and query strings must never become authoritative ambient context.

## Request context

Tenant/domain accessors read server-established values from HttpContext.Items. Middleware rejects accidental second initialization and clears execution, tenant and domain entries after downstream execution, including exceptional paths.

Compatibility: HTTP accessors no longer treat inbound tenant headers as current tenant state. Applications that previously read ambient tenant identity directly from headers must migrate to trusted resolution and the execution-context accessor. ITenantValidator.IsValidAsync supports cancellation.

## Swagger

The package provides an operation filter for documenting required tenant-specific request headers where an application uses those headers as resolver inputs. Document the semantics accurately: a request header is a selection hint, not an authorization credential.

## Dependencies and version

Uses Franz.Common.MultiTenancy and the Franz HTTP/header abstractions. Consult the project file for the exact dependency graph.

Framework release line: **2.3.4**. Target framework: net10.0.

See [tenant isolation audit](../../Docs/tenant-isolation-boundary-audit.md).

MIT License.
