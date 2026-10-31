using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.MultiTenancy;

public interface ITenantMessageContextInitializer
{
    Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default);

    Task<IAsyncDisposable> BeginAsync(Message message, CancellationToken cancellationToken = default);
}
