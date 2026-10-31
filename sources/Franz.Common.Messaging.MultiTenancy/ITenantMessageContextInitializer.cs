namespace Franz.Common.Messaging.MultiTenancy;

public interface ITenantMessageContextInitializer
{
    Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default);
}
