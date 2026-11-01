using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging.MultiTenancy;

public interface ITenantMessageContextInitializer
{
    Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default);

    Task<IAsyncDisposable> BeginAsync(Message message, CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "This tenant message initializer does not support explicit message execution. Implement the message overload before using it in transport consumers.");
}
