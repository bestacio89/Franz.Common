#nullable enable
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;

public abstract class HostedMessagingFixture<TContainer> : IAsyncLifetime
  where TContainer : IAsyncDisposable
{
  protected IHost? Host { get; private set; }
  protected TContainer? Container { get; private set; }

  private readonly TContainer? _externalContainer;

  protected HostedMessagingFixture() { }

  protected HostedMessagingFixture(TContainer externalContainer)
  {
    _externalContainer = externalContainer;
  }

  public IServiceProvider Services
    => Host?.Services
       ?? throw new InvalidOperationException("Host has not been started.");

  private bool _hostStarted;
  private bool _containerOwned;

  protected abstract TContainer CreateContainer();
  protected abstract IHost BuildHost(TContainer container);

  public async Task InitializeAsync()
  {
    if (_externalContainer is not null)
    {
      Container = _externalContainer;
      _containerOwned = false;
    }
    else
    {
      Container = CreateContainer();
      await (Container as dynamic).StartAsync();
      _containerOwned = true;
    }

    try
    {
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

    if (_containerOwned && Container is not null)
    {
      try { await Container.DisposeAsync(); }
      catch { }
    }
  }
}