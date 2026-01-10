
# 📡 **Franz.Common.Grpc**

### **v1.6.20 — .NET 10 Modernization Release**

High-performance, pipeline-driven gRPC for distributed microservices

---
- **Current Version**: 1.7.5

---

Franz.Common.Grpc brings the **full Franz architectural standard** to gRPC:

* Canonical behavior pipelines (Validation → Tenant → Auth → Logging → Metrics → Exceptions)
* Unified *GrpcCallContext* abstraction
* Client-side and server-side interceptors
* Behavior providers with ordered pipeline resolution
* Metadata normalization + context propagation
* Pure-core design with optional ASP.NET Core adapters
* First-class DI integration
* Flexible channel factory + service routing
* No-op defaults so services boot cleanly

This release is aligned with the **Franz v1.6.20 refactor** and `.NET 10` modernization.

---

# 🚀 **What’s New in v1.6.20**

### ✔ Complete folder and pipeline redesign

Franz.Common.Grpc is now structured into:

```
Abstractions/
Client/
Server/
Hosting/
DependencyInjection/
Configuration/
```

### ✔ Full Server Pipeline

Six canonical server interceptors:

1. **ValidationServerBehavior**
2. **TenantResolutionServerBehavior**
3. **AuthorizationServerBehavior**
4. **LoggingServerBehavior**
5. **MetricServerBehavior**
6. **ExceptionMappingServerBehavior**

### ✔ Full Client Pipeline

Equivalent client behaviors (correctly named):

* ValidationClientBehavior
* TenantResolutionClientBehavior
* AuthorizationClientBehavior
* LoggingClientBehavior
* MetricClientBehavior
* ExceptionMappingClientBehavior

### ✔ Behavior Providers (pipeline resolvers)

* `GrpcServerBehaviorProvider`
* `GrpcClientBehaviorProvider`

Both provide **ordered, cached** behavior pipelines per request/response pair.

### ✔ Unified GrpcCallContext

Core abstraction shared by both ends.

### ✔ Channel Factory

`FranzGrpcClientFactory` provides:

* Named service routing
* Channel creation
* Auto-configuration
* Timeout handling
* Optional metadata injection
* Safe instantiation of generated gRPC clients

### ✔ No-Op Defaults

Franz.Common.Grpc stays zero-config compatible:

* `NoOpValidationEngine`
* `NoOpAuthorizationService`
* `NoOpTenantResolver`
* `NoOpGrpcLogger`
* `NoOpGrpcMetrics`

### ✔ Pure Core — No ASP.NET Dependencies

All ASP.NET Core routing / MapGrpcService integrations are now moved to the upcoming package:

```
Franz.Common.Grpc.AspNetCore
```

### ✔ Configuration Cleanup

`FranzGrpcClientOptions` now includes:

```json
"Franz": {
  "Grpc": {
    "Client": {
      "Services": {
        "UserService": {
          "BaseAddress": "https://localhost:6001"
        }
      }
    }
  }
}
```

And a new type:

```
FranzGrpcClientServiceConfig
```

---

# 📁 **Final Project Structure (v1.6.20)**

```
Franz.Common.Grpc
│
├── Abstractions/
│     ├── IGrpcServerBehavior.cs
│     ├── IGrpcClientBehavior.cs
│     ├── IGrpcServerBehaviorProvider.cs
│     ├── IGrpcClientBehaviorProvider.cs
│     ├── IFranzGrpcClientFactory.cs
│     ├── IFranzValidationEngine.cs
│     ├── IFranzAuthorizationService.cs
│     ├── IFranzTenantResolver.cs
│     ├── IFranzGrpcLogger.cs
│     ├── IFranzGrpcMetrics.cs
│     ├── GrpcCallContext.cs
│
├── Client/
│     ├── GrpcClientBehaviorProvider.cs
│     ├── FranzGrpcClientFactory.cs
│     ├── Interceptors/
│           ├── ValidationClientBehavior.cs
│           ├── TenantResolutionClientBehavior.cs
│           ├── AuthorizationClientBehavior.cs
│           ├── LoggingClientBehavior.cs
│           ├── MetricClientBehavior.cs
│           ├── ExceptionMappingClientBehavior.cs
│
├── Server/
│     ├── GrpcServerBehaviorProvider.cs
│     ├── Interceptors/
│           ├── ValidationServerBehavior.cs
│           ├── TenantResolutionServerBehavior.cs
│           ├── AuthorizationServerBehavior.cs
│           ├── LoggingServerBehavior.cs
│           ├── MetricServerBehavior.cs
│           ├── ExceptionMappingServerBehavior.cs
│
├── Configuration/
│     ├── FranzGrpcOptions.cs
│     ├── FranzGrpcClientOptions.cs
│     ├── FranzGrpcClientServiceConfig.cs
│
├── Hosting/
│     ├── GrpcContextExtensions.cs
│     ├── NoOp/
│           ├── NoOpValidationEngine.cs
│           ├── NoOpAuthorizationService.cs
│           ├── NoOpTenantResolver.cs
│           ├── NoOpGrpcLogger.cs
│           ├── NoOpGrpcMetrics.cs
│
├── DependencyInjection/
│     ├── GrpcServiceCollectionExtensions.cs
│
└── (ASP.NET Core integration coming in `Franz.Common.Grpc.AspNetCore`)
```

---

# ⚙️ **Dependency Injection**

The core DI setup:

```csharp
builder.Services.AddFranzGrpcDefaults();
builder.Services.AddFranzGrpcServer(configuration);
builder.Services.AddFranzGrpcClient(configuration);
```

You must configure:

```csharp
builder.Services.AddGrpc();              // ASP.NET Core host only
builder.Services.AddGrpcClient<TClient>();  // for each generated client
```

These calls DO NOT live in Franz.Common.Grpc.

---

# 🧠 **Design Principles**

### ✔ Pure-Core Philosophy

No reference to ASP.NET Core inside the core library.

### ✔ Canonical Pipeline Ordering

The same strict behavior order as Franz.Common.Mediator, Messaging, HTTP.

### ✔ 100% Predictable Execution

Every call passes through the same ordered pipeline.

### ✔ First-Class Context

GrpcCallContext replaces ServerCallContext to ensure:

* correlationId
* requestId
* tenantId
* userId
* serviceName
* methodName
* deadlines
* cancellation

Are all unified between client and server.

### ✔ Behavior-Driven Architecture

Like Middleware but transport-agnostic.

### ✔ Adaptable

Client & Server behaviors use generics:

```
IGrpcClientBehavior<TRequest, TResponse>
IGrpcServerBehavior<TRequest, TResponse>
```

This allows typesafe interception at compile time.

---

# 🌐 **Future: Franz.Common.Grpc.AspNetCore**

All of the following will move to the integration package:

* MapFranzGrpcService
* EndpointRouteBuilderExtensions
* UseFranzGrpc middleware
* Diagnostics + correlation pipeline
* gRPC JSON transcoding support
* OpenAPI & API explorer integration

Core remains pure, integration becomes optional.

---

# 🏁 **Conclusion**

`Franz.Common.Grpc v1.6.20` delivers:

* A complete microservice-grade gRPC pipeline
* Clean architecture layering
* Fully aligned behavior systems across Franz (Mediator, Messaging, Http, Grpc)
* Modernized .NET 10 compatibility
* Clear separation between **core** and **hosting adapters**
* Production grade DI, context, factory, and routing design

This makes Franz one of the **strictest, most consistent, and most architecturally coherent** .NET gRPC stacks available.

---

