using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.MultiTenancy;

public sealed class TenantMessageContextInitializer(
    IMessageContextAccessor messageContextAccessor,
    ITenantStore tenantStore,
    ITenantExecutionContextAccessor executionContextAccessor,
    ITenantMessageAuthorizer authorizer,
    IServiceProvider serviceProvider) : ITenantMessageContextInitializer
{
    public async Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default)
    {
        var message = messageContextAccessor.Current?.Message
            ?? throw new TenantResolutionException("A current message is required to establish tenant context.");
        var tenantId = ReadRequiredGuid(message, HeaderConstants.TenantId, "tenant");
        var tenant = await tenantStore.FindByIdAsync(tenantId).ConfigureAwait(false)
            ?? throw new TenantResolutionException("The message references an unknown tenant.");
        if (tenant.Status != TenantStatus.Active)
            throw new TenantResolutionException("The message tenant is not active.");

        var validator = serviceProvider.GetService<ITenantValidator>();
        if (validator is not null && !await validator.IsValidAsync(tenant.Id, cancellationToken).ConfigureAwait(false))
            throw new TenantResolutionException("The message tenant failed validation.");
        if (!await authorizer.IsAuthorizedAsync(tenant, message, cancellationToken).ConfigureAwait(false))
            throw new TenantResolutionException("The message producer is not authorized for the resolved tenant.");

        Guid? domainId = null;
        if (message.Headers.ContainsKey(HeaderConstants.DomainId))
        {
            var candidateDomainId = ReadRequiredGuid(message, HeaderConstants.DomainId, "domain");
            var domain = tenant.Domains.SingleOrDefault(item => item.Id == candidateDomainId && item.IsVerified &&
                (item.TenantId == Guid.Empty || item.TenantId == tenant.Id));
            if (domain is null)
                throw new TenantResolutionException("The message domain does not belong to the resolved tenant or is not verified.");
            domainId = domain.Id;
        }

        var context = new TenantExecutionContext(tenant, domainId, TenantResolutionSource.Header, DateTimeOffset.UtcNow);
        executionContextAccessor.Initialize(context);
        return new TenantMessageExecutionScope(executionContextAccessor);
    }

    private static Guid ReadRequiredGuid(Message message, string headerName, string identityName)
    {
        if (!message.Headers.TryGetValue(headerName, out var values) || values.Length != 1 ||
            !Guid.TryParse(values[0], out var value) || value == Guid.Empty)
            throw new TenantResolutionException($"The message must contain exactly one valid {identityName} identifier.");
        return value;
    }

    private sealed class TenantMessageExecutionScope(ITenantExecutionContextAccessor accessor) : IAsyncDisposable
    {
        private bool _disposed;
        public ValueTask DisposeAsync()
        {
            if (!_disposed) { accessor.Clear(); _disposed = true; }
            return ValueTask.CompletedTask;
        }
    }
}
