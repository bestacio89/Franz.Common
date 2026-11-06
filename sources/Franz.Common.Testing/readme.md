# Franz.Common.Testing

> Test base class, strict Moq verification, and a lightweight Mediator test dispatcher.

## Overview

The package provides a reusable unit-test base class and a lightweight in-memory Mediator dispatcher for focused handler and pipeline tests. Its dispatcher is intentionally test-only and does not implement the complete production dispatch surface.

## Installation

~~~bash
dotnet add package Franz.Common.Testing
~~~

## Included utilities

### UnitTest

UnitTest is a disposable base class that creates a strict Moq MockRepository and sets the current thread's CurrentCulture and CurrentUICulture. The default constructor uses the package's configured default culture; the string constructor accepts an explicit culture name.

On disposal, UnitTest calls MockRepository.VerifyAll(). Tests must configure and verify the expected interactions before the test fixture is disposed.

~~~csharp
using FluentAssertions;
using Franz.Common.Testing;
using Xunit;

public sealed class OrderServiceTests : UnitTest
{
    [Fact]
    public void Example()
    {
        var value = 1;

        value.Should().Be(1);
    }
}
~~~

### TestDispatcher

TestDispatcher is a lightweight in-memory IDispatcher implementation for focused unit tests. It is configured fluently with handlers and pipeline components:

~~~csharp
using Franz.Common.Testing.Mediator;

var dispatcher = new TestDispatcher()
    .WithHandler<GetOrderQuery, OrderDto>(new GetOrderHandler())
    .WithHandler<OrderCreatedNotification>(new OrderCreatedHandler())
    .WithPipeline<GetOrderQuery, OrderDto>(new TestLoggingPipeline());
~~~

Supported configuration methods include WithHandler, WithPipeline, WithPreProcessor, WithPostProcessor, and WithNotificationPipeline.

The current implementation supports typed command/query dispatch, notification publication, and stream query enumeration for the corresponding registered handler types. It uses dynamic invocation and is explicitly test-only; it is annotated with RequiresDynamicCode and RequiresUnreferencedCode.

## Current dispatcher coverage limitations

TestDispatcher is not a complete implementation of every IDispatcher member:

- PublishEventAsync currently throws NotImplementedException.
- Send(INotification) currently throws NotImplementedException.
- The non-generic SendAsync(ICommand) wrapper path is not a supported void-command test path in the current implementation.

Use the real FranzDispatcher with generated registrations for event dispatch, void-command integration tests, and behavior that depends on the full Mediator runtime. Do not use TestDispatcher as evidence of Native AOT compatibility.

## Dependencies

The package references:

- FluentAssertions 8.10.0;
- Moq.AutoMock 4.0.2;
- MockQueryable.Moq 10.0.8;
- Franz.Common.Mediator.

## Testing guidance

Use TestDispatcher for isolated handler/pipeline tests where its supported dispatch surface is sufficient. Use the production DI registration and generated Mediator invokers for integration tests. Use real persistence and transport adapters for their respective integration tests rather than relying on mocked IQueryable behavior as a substitute for provider translation.

## License

MIT. See the repository [LICENSE](../../LICENSE).
