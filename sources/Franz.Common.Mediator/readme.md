Here’s a **full draft of a `README.md`** for `Franz.Common.Mediator` that captures *everything we’ve built together* — enterprise-grade mediator with options, pipelines, observability, context, results, testing, and extensibility. It’s written as a clean, developer-friendly doc with code snippets inline.

---

# Franz.Common.Mediator

Franz.Common.Mediator is a **production-grade mediator library** for .NET that goes beyond MediatR.
It’s **framework-agnostic, configurable, observable, resilient, and testable** — built for real enterprise systems.

Unlike minimal mediators, Franz ships with:

* Clean **contracts** (commands, queries, notifications, streams).
* Plug-and-play **pipelines** for logging, validation, retry, caching, transactions, circuit breakers, bulkheads, and more.
* **Options-driven configuration** (no hardcoded values).
* Built-in **observability** with correlation IDs, multi-tenant context, and per-handler telemetry.
* Unified **Result/Error** handling with structured metadata.
* A lightweight **TestDispatcher** for easy unit testing.

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mediator
```

---

## 🚀 Quick Start

### 1. Define a Command and Handler

```csharp
public record CreateUserCommand(string Username, string Email) : ICommand<Result<Guid>>;

public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<Guid>.Failure("Invalid email");

        return Result<Guid>.Success(Guid.NewGuid());
    }
}
```

### 2. Wire Mediator in DI

```csharp
services.AddFranzMediator(options =>
{
    options.Retry.MaxAttempts = 3;
    options.Timeout.Duration = TimeSpan.FromSeconds(2);
    options.CircuitBreaker.FailureThreshold = 5;
    options.Bulkhead.MaxConcurrentRequests = 20;
    options.Caching.DefaultTtl = TimeSpan.FromMinutes(5);

    options.EnableDefaultConsoleObserver = true; // for demo/test
});
```

### 3. Dispatch from your app

```csharp
var result = await dispatcher.Send(new CreateUserCommand("bob", "bob@example.com"));

if (result.IsSuccess)
    Console.WriteLine($"Created user {result.Value}");
else
    Console.WriteLine($"Failed: {result.Error.Message}");
```

---

## 🔧 Features

### ✅ Commands & Queries

```csharp
public record GetUserQuery(Guid Id) : IQuery<Result<User>>;

public class GetUserHandler : IQueryHandler<GetUserQuery, Result<User>>
{
    public Task<Result<User>> Handle(GetUserQuery query, CancellationToken ct) =>
        Task.FromResult(Result<User>.Success(new User(query.Id, "bob")));
}
```

### 📣 Notifications

```csharp
public record UserCreatedEvent(Guid Id, string Email) : INotification;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    public Task Handle(UserCreatedEvent ev, CancellationToken ct)
    {
        Console.WriteLine($"Welcome {ev.Email}!");
        return Task.CompletedTask;
    }
}
```

Dispatcher supports:

* Sequential or Parallel publish strategies.
* Error policies (`StopOnFirstFailure` / `ContinueOnError`).
* Per-handler telemetry.

```csharp
await dispatcher.PublishAsync(new UserCreatedEvent(userId, email),
    PublishStrategy.Parallel,
    NotificationErrorHandling.ContinueOnError);
```

### 🌊 Streaming Queries

```csharp
public record GetNumbersStream(int Count) : IStreamQuery<int>;

public class GetNumbersStreamHandler : IStreamQueryHandler<GetNumbersStream, int>
{
    public async IAsyncEnumerable<int> Handle(GetNumbersStream q, [EnumeratorCancellation] CancellationToken ct)
    {
        for (int i = 0; i < q.Count; i++)
        {
            yield return i;
            await Task.Delay(100, ct);
        }
    }
}
```

```csharp
await foreach (var n in dispatcher.Stream(new GetNumbersStream(5)))
    Console.WriteLine(n);
```

---

## 🧩 Pipelines (Cross-Cutting Concerns)

Franz ships with many built-in pipelines, all **options-driven**:

* **LoggingPipeline** → request/response logging.
* **ValidationPipeline** → runs all `IValidator<TRequest>`.
* **RetryPipeline** → retry transient errors.
* **TimeoutPipeline** → cancel long-running requests.
* **CircuitBreakerPipeline** → stop calling failing handlers.
* **BulkheadPipeline** → limit concurrent requests.
* **CachingPipeline** → cache query results.
* **TransactionPipeline** → commit/rollback with `IUnitOfWork`.

Example:

```csharp
public class RetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
    private readonly RetryOptions _options;

    public RetryPipeline(RetryOptions options) => _options = options;

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        for (int i = 0; i < _options.MaxAttempts; i++)
        {
            try { return await next(); }
            catch when (i < _options.MaxAttempts - 1)
            {
                await Task.Delay(_options.Delay, ct);
            }
        }
        throw new Exception("Retries exhausted.");
    }
}
```

---

## 📋 Options Pattern

All pipelines are driven by central options:

```csharp
public class FranzMediatorOptions
{
    public RetryOptions Retry { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public BulkheadOptions Bulkhead { get; set; } = new();
    public CachingOptions Caching { get; set; } = new();
    public TransactionOptions Transaction { get; set; } = new();

    public bool EnableDefaultConsoleObserver { get; set; }
}
```

---

## 🔍 Observability & Context

Every request/notification/stream is observable via `IMediatorObserver`.

```csharp
public class ConsoleMediatorObserver : IMediatorObserver
{
    public Task OnRequestStarted(Type req, string correlationId) =>
        Task.Run(() => Console.WriteLine($"➡ {req.Name} started [{correlationId}]"));

    public Task OnRequestCompleted(Type req, string correlationId, TimeSpan duration) =>
        Task.Run(() => Console.WriteLine($"✅ {req.Name} completed in {duration.TotalMs()} ms"));

    public Task OnRequestFailed(Type req, string correlationId, Exception ex) =>
        Task.Run(() => Console.WriteLine($"❌ {req.Name} failed: {ex.Message}"));
}
```

### MediatorContext

Available everywhere in pipelines/handlers:

```csharp
MediatorContext.Current.UserId
MediatorContext.Current.TenantId
MediatorContext.Current.Culture
MediatorContext.Current.CorrelationId
```

Populate automatically with ASP.NET Core middleware:

```csharp
app.UseMediatorContext();
```

---

## ❗ Error & Result Handling

Every handler returns a `Result` or `Result<T>`.

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    // ...
}

public class Result<T> : Result
{
    public T? Value { get; }
}
```

### Example

```csharp
if (!result.IsSuccess)
{
    Console.WriteLine(result.Error.Code);     // e.g., "ValidationError"
    Console.WriteLine(result.Error.Message);  // e.g., "Email is required"
}
```

Errors carry codes and metadata for structured responses.

---

## 🧪 Testing

Use the `TestDispatcher` to run handlers without DI or full setup.

```csharp
var dispatcher = new TestDispatcher()
    .WithHandler(new CreateUserHandler())
    .WithPipeline(new LoggingPipeline<,>())
    .WithPreProcessor(new LoggingPreProcessor<>());

var result = await dispatcher.Send(new CreateUserCommand("bob", "bob@example.com"));

Assert.True(result.IsSuccess);
```

---

## 🌐 ASP.NET Core Integration

In a Web API, convert mediator results to HTTP results with an adapter:

```csharp
app.MapPost("/users", async (CreateUserCommand cmd, IDispatcher dispatcher) =>
{
    var result = await dispatcher.Send(cmd);
    return result.ToIResult(); // Ok() or Problem()
});
```

---

## 📐 Design Principles

* **Framework-agnostic** → core never references ASP.NET, EF, Mongo, etc.
* **Contracts only** → no adapters included, devs own infra details.
* **Options-driven** → configure via DI/appsettings.
* **Observable** → correlation IDs, telemetry hooks, multi-tenant context.
* **Resilient** → retries, timeouts, bulkheads, circuit breakers built-in.
* **Testable** → TestDispatcher and fakes included.

---

## 📜 License

MIT

---

## 📝 Changelog

### v1.3.2

* Introduced Options pattern (`Retry`, `Timeout`, `CircuitBreaker`, `Bulkhead`, `Caching`, `Transaction`).
* Upgraded pipelines to be options-aware.
* Added `MediatorContext` with user/tenant/culture.
* Expanded observability with `IMediatorObserver` and per-handler telemetry.
* Unified `Result`/`Result<T>` error handling.
* Added Validation with structured errors + FluentValidation adapter.
* Introduced `TestDispatcher` for unit testing.
* Added `AddFranzMediator()` DI extension with automatic scanning.
* Default `ConsoleMediatorObserver` for demo/testing.

---
## [1.3.3] - 2025-09-15
### Added
- Introduced `Error` value object (`Franz.Common.Mediator.Errors.Error`) with factory helpers:
  - `Error.NotFound`, `Error.Validation`, `Error.Conflict`, `Error.Unexpected`.
- Added `ErrorCodes` constants for consistency.
- Updated `Result` and `Result<T>` to integrate with `Error`.
- Updated ASP.NET Core adapter to map `Error` codes to proper HTTP responses:
  - NotFound → 404
  - Validation → 400
  - Conflict → 409
  - Unexpected/default → 500

