# Franz.Common.Testing

> Lightweight unit-test base class and test-only Mediator dispatcher helpers.

## Overview

This package contains a reusable `UnitTest` base class and a lightweight in-memory `TestDispatcher`. It is a test utility package, not a production dispatcher or a complete replacement for the production Mediator runtime.

## Installation

```bash
dotnet add package Franz.Common.Testing
```

## UnitTest base class

Derive a test fixture from `UnitTest` to get a strict Moq `MockRepository` and a deterministic current/UI culture:

```csharp
public sealed class CustomerServiceTests : UnitTest
{
    [Fact]
    public void Example()
    {
        var dependency = MockRepository.Create<ICustomerDependency>();
        // Arrange, act, assert.
    }
}
```

The default constructor uses the package's default culture. An overload accepts an explicit culture name. On disposal, the base class calls `MockRepository.VerifyAll()`.

## TestDispatcher

`Franz.Common.Testing.Mediator.TestDispatcher` supports direct registration of command, query, notification, and stream-query handlers, plus selected request/notification pipelines and pre/post-processors.

```csharp
var dispatcher = new TestDispatcher()
    .WithHandler<GetCustomerQuery, CustomerDto>(handler);

var result = await dispatcher.SendAsync<CustomerDto>(
    new GetCustomerQuery(customerId),
    cancellationToken);
```

The dispatcher uses dynamic invocation and is annotated as requiring dynamic code and unreferenced members. It is intended for unit tests and is not Native AOT compatible.

**Coverage boundary:** the current implementation throws `NotImplementedException` for `Send(INotification)` and `PublishEventAsync`. Its non-generic `SendAsync(ICommand)` wrapper path also does not resolve the original command handler reliably. Do not use those paths in tests expecting production-equivalent dispatch. Use the production dispatcher in integration tests for behavior that depends on the full dispatch surface.

## License

MIT. See the repository [LICENSE](../../LICENSE).
