#nullable enable
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Probes;
using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Executing;

public sealed class TestMessagingStrategyExecuter : IMessagingStrategyExecuter
{
  private readonly ITestProbe _probe;
  private readonly MessageContextAccessor _contextAccessor;

  public TestMessagingStrategyExecuter(ITestProbe probe, MessageContextAccessor contextAccessor)
  {
    _probe = probe;
    _contextAccessor = contextAccessor;
  }


  public Task<bool> CanExecuteAsync(Message message)
      => Task.FromResult(message is not null);

  public Task ExecuteAsync(Message message)
  {
   
    _probe.SignalArrival(message.Id);
    return Task.CompletedTask;
  }
}