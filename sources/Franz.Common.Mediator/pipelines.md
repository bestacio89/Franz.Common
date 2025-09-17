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
