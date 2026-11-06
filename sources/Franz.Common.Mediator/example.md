# Mediator application example

This guide demonstrates the supported generated-registration path for an application command and query. It complements the [package guide](readme.md), which documents registration modes, supported handler contracts, pipeline modules, and Native AOT boundaries.

## 1. Define request contracts

Keep requests immutable and give each request one explicit handler contract.

```csharp
public sealed record CreateOrderCommand(Guid OrderId, decimal Total)
    : ICommand<bool>;

public sealed record GetOrderQuery(Guid OrderId)
    : IQuery<OrderDto>;

public sealed record OrderDto(Guid Id, decimal Total);
```

## 2. Implement handlers

```csharp
public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrderCommand, bool>
{
    private readonly OrdersDbContext _db;

    public CreateOrderHandler(OrdersDbContext db) => _db = db;

    public async Task<bool> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        _db.Orders.Add(new Order(request.OrderId, request.Total));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class GetOrderHandler
    : IQueryHandler<GetOrderQuery, OrderDto>
{
    private readonly OrdersDbContext _db;

    public GetOrderHandler(OrdersDbContext db) => _db = db;

    public async Task<OrderDto> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .SingleAsync(x => x.Id == request.OrderId, cancellationToken);

        return new OrderDto(order.Id, order.Total);
    }
}
```

The example assumes an application-owned `OrdersDbContext` and `Order` model. Persistence registration and transaction ownership remain application concerns.

## 3. Register Mediator

For the generated, closed-generic default pipeline composition, select an assembly through a marker type:

```csharp
using Franz.Common.Mediator.Extensions;

builder.Services.AddFranzMediatorDefault<OrdersApplicationAssembly>();
```

The marker type can be an otherwise empty type declared in the assembly containing the handlers. The source generator contributes handler registrations at build time; the application references `Franz.Common.Mediator`, which carries the generator as an analyzer asset.

This default composition includes request logging, validation, Serilog logging, transaction pipelines, and the built-in logging/audit pre- and post-processors. Notifications receive notification logging and validation. It does not add event validation or resilience pipelines.

If you need only core infrastructure and explicit generated registration, use:

```csharp
builder.Services.AddFranzMediator(
    new[] { typeof(OrdersApplicationAssembly).Assembly });
```

See [pipelines.md](pipelines.md) before choosing a different pipeline composition. In particular, `AddFranzMediatorV2Default` and `AddFranzMediatorStandard` use open-generic pipelines and are not Native AOT compositions.

## 4. Dispatch from an application scope

Resolve the scoped dispatcher from the request scope:

```csharp
var dispatcher = scope.ServiceProvider
    .GetRequiredService<IDispatcher>();

var created = await dispatcher.SendAsync<bool>(
    new CreateOrderCommand(orderId, total),
    cancellationToken);

var order = await dispatcher.SendAsync<OrderDto>(
    new GetOrderQuery(orderId),
    cancellationToken);
```

Use `PublishNotificationAsync` for notifications and `PublishEventAsync` for domain/integration events. These are separate contracts; do not substitute one for the other.

## 5. Verify the consuming application

The package's generated registration and invoker path is designed for trimming and Native AOT. Keep the following checks in the application:

- Build the consuming application with the same SDK and publish settings used in deployment.
- Publish with trimming and Native AOT enabled when those are deployment requirements.
- Execute the published artifact; a successful build alone does not prove runtime compatibility.
- Add integration tests for the application's actual persistence, validation, transaction, and handler-lifetime behavior.

The repository's AOT smoke test covers selected generated Mediator paths. It does not make the open-generic V2-default or standard pipeline compositions AOT-compatible.
