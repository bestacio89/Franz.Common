# Franz.Common.Grpc

> gRPC client/server execution behaviors, call context, tenant propagation, and typed client factory support.

## Overview

Franz.Common.Grpc provides a host-independent gRPC behavior layer. It does not register ASP.NET Core gRPC hosting, endpoint routing, or generated typed clients on behalf of the application. The host remains responsible for AddGrpc() and AddGrpcClient<TClient>().

## Installation

~~~bash
dotnet add package Franz.Common.Grpc
~~~

## Dependency injection

Register defaults when the application wants the built-in no-op validation, authorization, tenant-resolution, logging, and metrics services. Then register the side(s) used by the application:

~~~csharp
using Franz.Common.Grpc.DependencyInjection;

services.AddFranzGrpcDefaults();
services.AddFranzGrpcServer(configuration);
services.AddFranzGrpcClient(configuration);
~~~

- AddFranzGrpcServer binds Franz:Grpc options, registers the server behaviors in validation, tenant resolution, authorization, logging, metrics, and exception-mapping order, and registers the server behavior provider.
- AddFranzGrpcClient binds Franz:Grpc:Client options, registers the tenant-resolution client behavior, client behavior provider, and Franz gRPC client factory.
- AddFranzGrpcDefaults registers no-op implementations for validation, authorization, tenant resolution, logging, and metrics. Replace them with application implementations where real enforcement or telemetry is required.

The application host separately configures the underlying gRPC stack:

~~~csharp
services.AddGrpc();                 // ASP.NET Core server host
services.AddGrpcClient<MyClient>(); // typed client registration
~~~

## Configuration

Global options are bound from Franz:Grpc. They control correlation ID generation, default timeout, metadata key normalization, identity propagation, tenant requirements, payload logging, and Franz metadata prefix.

Client options are bound from Franz:Grpc:Client. They control service name, default client timeout, metadata injection, retry settings, payload logging, method-name resolution, and the named Services configuration used by FranzGrpcClientFactory.

Payload logging is disabled by default. Do not enable it for sensitive request or response data without an explicit data-handling review.

## Call context and behaviors

GrpcCallContext carries request metadata used by the client and server behavior pipelines, including correlation, request, tenant, user, service, method, deadline, and cancellation information.

The server behavior provider composes the registered IGrpcServerBehavior<TRequest,TResponse> implementations. The client behavior provider composes IGrpcClientBehavior<TRequest,TResponse> implementations. The default client registration currently adds tenant-resolution propagation; do not assume that the server's complete behavior set is automatically registered on the client.

## Tenant security boundary

The server tenant behavior removes caller-supplied x-tenant-id and x-user-id metadata before invoking the application-provided IFranzTenantResolver. A resolved tenant identifier must parse as a non-empty GUID.

When a tenant is resolved, the server requires IFranzTenantAuthorizer and a tenant execution-context accessor. Authorization must use trusted caller identity; caller-supplied tenant/user metadata is not proof of identity or membership. When RequireTenant is enabled, ITenantValidator must also be registered and the resolved tenant must pass validation. Missing or denied authorization fails closed.

If an HTTP middleware has already established a validated tenant execution context for the same request, the gRPC behavior reuses that context and leaves cleanup to the HTTP request scope. Otherwise it initializes and clears its own execution context around the gRPC call.

The client behavior propagates tenant identity only from a validated ITenantExecutionContextAccessor. When no validated tenant context exists, caller-provided tenant metadata is removed. A legacy resolver-based constructor remains for compatibility; new integrations should use the validated execution-context path.

## Operational guidance

- Treat client retry as an opt-in policy; only retry operations whose repeated execution is safe.
- Propagate cancellation and deadlines to downstream operations.
- Keep payload logging disabled unless data classification permits it.
- Register real authorization and tenant validation services when tenant enforcement is enabled.
- Test client and server behaviors independently from ASP.NET Core hosting.

## Related packages

- Franz.Common.MultiTenancy — shared tenant execution-context contracts.
- Franz.Common.Http.MultiTenancy — HTTP request resolution and authorization.
- Franz.Common.Messaging.MultiTenancy — message delivery tenant context.

## License

MIT. See the repository [LICENSE](../../LICENSE).
