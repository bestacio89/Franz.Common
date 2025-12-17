using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Fixtures;

public abstract class HostedMessagingFixture<TContainer> : IAsyncLifetime
  where TContainer : IAsyncDisposable
{
  protected IHost? Host { get; private set; }
  protected TContainer? Container { get; private set; }

  public IServiceProvider Services
    => Host?.Services
       ?? throw new InvalidOperationException("Host has not been started.");

  private bool _hostStarted;
  private bool _containerStarted;

  protected abstract TContainer CreateContainer();
  protected abstract IHost BuildHost(TContainer container);

  public async Task InitializeAsync()
  {
    Container = CreateContainer();

    try
    {
      await (Container as dynamic).StartAsync();
      _containerStarted = true;

      Host = BuildHost(Container);
      await Host.StartAsync();
      _hostStarted = true;
    }
    catch
    {
      await SafeDisposeAsync();
      throw;
    }
  }

  public async Task DisposeAsync()
    => await SafeDisposeAsync();

  private async Task SafeDisposeAsync()
  {
    if (_hostStarted && Host is not null)
    {
      try { await Host.StopAsync(); }
      catch { }
      finally { Host.Dispose(); }
    }

    if (_containerStarted && Container is not null)
    {
      try { await Container.DisposeAsync(); }
      catch { }
    }
  }
}
