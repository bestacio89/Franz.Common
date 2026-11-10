using FluentAssertions;
using Franz.Common.Mediator;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Core;

namespace Franz.Common.Messaging.Sagas.Tests.Validation;

public sealed class SagaRouterMessageTypeResolutionTests
{
  [Fact]
  public void TryResolveMessageType_ResolvesRegisteredAssemblyQualifiedType()
  {
    var router = CreateRouter();

    var resolved = router.TryResolveMessageType(
        typeof(RegisteredEvent).AssemblyQualifiedName!,
        out var messageType);

    resolved.Should().BeTrue();
    messageType.Should().Be(typeof(RegisteredEvent));
  }

  [Fact]
  public void TryResolveMessageType_ResolvesCanonicalMessagingHeaderName()
  {
    var router = CreateRouter();
    var canonicalName = HeaderNamer.GetEventClassName(typeof(RegisteredEvent));

    var resolved = router.TryResolveMessageType(canonicalName, out var messageType);

    resolved.Should().BeTrue();
    messageType.Should().Be(typeof(RegisteredEvent));
  }

  [Fact]
  public void TryResolveMessageType_ResolvesRegisteredFullName()
  {
    var router = CreateRouter();

    var resolved = router.TryResolveMessageType(
        typeof(RegisteredEvent).FullName!,
        out var messageType);

    resolved.Should().BeTrue();
    messageType.Should().Be(typeof(RegisteredEvent));
  }

  [Theory]
  [InlineData("System.String")]
  [InlineData("System.IO.FileInfo, System.IO.FileSystem")]
  [InlineData("Unregistered.IntegrationEvent, Untrusted.Assembly")]
  public void TryResolveMessageType_RejectsTypesOutsideRegisteredSagaContracts(string typeName)
  {
    var router = CreateRouter();

    var resolved = router.TryResolveMessageType(typeName, out var messageType);

    resolved.Should().BeFalse();
    messageType.Should().BeNull();
  }

  private static SagaRouter CreateRouter()
  {
    var router = new SagaRouter(new EmptyServiceProvider());
    router.RegisterSaga(typeof(RegisteredSaga));
    return router;
  }

  private sealed class RegisteredSaga : SagaBase<RegisteredSagaState>, IStartWith<RegisteredEvent>
  {
    public Task<ISagaTransition> HandleAsync(
        RegisteredEvent message,
        ISagaContext context,
        CancellationToken cancellationToken)
        => Task.FromResult(context.Continue());
  }

  private sealed class RegisteredSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
  }

  private sealed record RegisteredEvent : IIntegrationEvent;

  private sealed class EmptyServiceProvider : IServiceProvider
  {
    public object? GetService(Type serviceType) => null;
  }
}
