# 🧩 Full Example: Pipelines with Franz.Common.Mediator

This example shows how to use **multiple pipelines together** — logging, retry, caching, and transactions — wrapped around a single command.

---

## 📦 Step 1. Define a Command + Handler

```csharp
// A simple command
public record CreateOrderCommand(Guid OrderId, decimal Amount) : ICommand<bool>;

// The handler that saves to DB
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, bool>
{
    private readonly OrdersDbContext _db;

    public CreateOrderHandler(OrdersDbContext db) => _db = db;

    public async Task<bool> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        _db.Orders.Add(new Order { Id = request.OrderId, Amount = request.Amount });
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

---

## 🛠 Step 2. Add Pipelines

### Logging

```csharp
public class LoggingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
    private readonly ILogger<LoggingPipeline<TRequest, TResponse>> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline<TRequest, TResponse>> logger) 
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Request} with {Response}", typeof(TRequest).Name, typeof(TResponse).Name);
        return response;
    }
}
```

### Retry (Polly)

```csharp
public class PollyRetryPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
    private readonly AsyncPolicy _policy;

    public PollyRetryPipeline(RetryOptions options)
    {
        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(options.MaxRetries, _ => options.Delay);
    }

    public Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
        => _policy.ExecuteAsync(() => next());
}
```

### Caching

```csharp
public class CachingPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;

    public CachingPipeline(IMemoryCache cache, CachingOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        var key = $"{typeof(TRequest).Name}:{request.GetHashCode()}";
        if (_cache.TryGetValue<TResponse>(key, out var cached))
            return cached;

        var result = await next();
        _cache.Set(key, result, _options.Duration);
        return result;
    }
}
```

### Transaction

```csharp
public class TransactionPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
{
    private readonly OrdersDbContext _db;

    public TransactionPipeline(OrdersDbContext db) => _db = db;

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
```

---

## 🧩 Step 3. Register in DI

```csharp
services.AddDbContext<OrdersDbContext>(...);
services.AddMemoryCache();

services.AddFranzMediator(typeof(CreateOrderHandler).Assembly, options =>
{
    options.Retry.MaxRetries = 3;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Caching.Duration = TimeSpan.FromMinutes(1);
});

services.AddScoped(typeof(IPipeline<,>), typeof(LoggingPipeline<,>));
services.AddScoped(typeof(IPipeline<,>), typeof(PollyRetryPipeline<,>));
services.AddScoped(typeof(IPipeline<,>), typeof(CachingPipeline<,>));
services.AddScoped(typeof(IPipeline<,>), typeof(TransactionPipeline<,>));
```

---

## 🚀 Step 4. Use the Mediator

```csharp
public class OrdersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public OrdersController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken ct)
    {
        var success = await _dispatcher.Dispatch(command, ct);
        return success ? Ok() : BadRequest();
    }
}
```

---

## 🔄 Execution Order

When `CreateOrderCommand` is dispatched, the pipeline chain executes as:

1. **LoggingPipeline** → logs start
2. **PollyRetryPipeline** → retries on transient errors
3. **CachingPipeline** → short-circuits if cached
4. **TransactionPipeline** → wraps DB in transaction
5. **Handler** → actually executes

---


# Franz Mediator with Resilience Pipelines

Franz Mediator allows you to compose request/response pipelines with opt-in behaviors like validation, logging, transactions, and resilience (retry, circuit breaker, timeout, bulkhead).

## 🔧 Installation

```csharp
dotnet add package Franz.Common.Mediator
```

---

## 📑 Configuration (appsettings.json)

```jsonc
{
  "Resilience": {
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelay": "00:00:02",     // 2 seconds
      "VerboseLogging": true,
      "Disabled": false
    },
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "OpenDuration": "00:00:10", // stays open 10s after trip
      "VerboseLogging": true,
      "Disabled": false
    },
    "Timeout": {
      "Duration": "00:00:05",
      "VerboseLogging": true,
      "Disabled": false
    },
    "Bulkhead": {
      "MaxConcurrentRequests": 10,
      "MaxQueueLength": 50,
      "VerboseLogging": true,
      "Disabled": false
    }
  }
}
```

---

## 🏗 Program.cs Setup

```csharp
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Pipelines.Resilience;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Bind resilience options from configuration
builder.Services.Configure<RetryOptions>(
    builder.Configuration.GetSection("Resilience:Retry"));
builder.Services.Configure<CircuitBreakerOptions>(
    builder.Configuration.GetSection("Resilience:CircuitBreaker"));
builder.Services.Configure<TimeoutOptions>(
    builder.Configuration.GetSection("Resilience:Timeout"));
builder.Services.Configure<BulkheadOptions>(
    builder.Configuration.GetSection("Resilience:Bulkhead"));

// Register Franz Mediator + pipelines
builder.Services.AddFranzMediator(
    new[] { typeof(Program).Assembly } // scan handlers in this assembly
);

builder.Services
    .AddFranzRetryPipeline()
    .AddFranzCircuitBreakerPipeline()
    .AddFranzTimeoutPipeline()
    .AddFranzBulkheadPipeline()
    .AddFranzValidationPipeline()
    .AddFranzLoggingPipeline();

var app = builder.Build();
app.Run();
```

---

## 📊 Example Logs (with a chaos exception)

```txt
[Pipeline:Retry] Attempt 1/3 after 2s due to 🍌 Banana Republic Exception: simulated DB meltdown!
[Pipeline:Retry] Attempt 2/3 after 4s due to 🍌 Banana Republic Exception: simulated DB meltdown!
[Pipeline:CircuitBreaker] OPEN after 5 consecutive failures
[Pipeline:Timeout] Request failed after 5s (timeout exceeded)
[Pipeline:Bulkhead] Rejected request — too many concurrent executions
```

---

## ✅ Summary

* **Config-driven** resilience options (`appsettings.json`).
* **Opt-in pipelines** via `AddFranzXxxPipeline()` extension methods.
* **Unified logging** integrated with `ILogger`/Serilog.
* Perfect for demos, tests, or chaos engineering (with “Banana Republic Exception” 😅).

---

👉 Do you want me to also include a **short handler example** (`GetBookByIdQueryHandler`) that throws one of your chaos exceptions so the retries/circuit breaker demo makes sense right in the README?
