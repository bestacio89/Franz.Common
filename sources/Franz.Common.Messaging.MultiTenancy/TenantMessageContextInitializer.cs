using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Messaging.MultiTenancy;

public sealed class TenantMessageContextInitializer : ITenantMessageContextInitializer
{
    private readonly IMessageContextAccessor? _messageContextAccessor;
    private readonly ITenantStore _tenantStore;
    private readonly ITenantExecutionContextAccessor _executionContextAccessor;
    private readonly ITenantMessageAuthorizer _authorizer;
    private readonly IServiceProvider _serviceProvider;

    public TenantMessageContextInitializer(
        ITenantStore tenantStore,
        ITenantExecutionContextAccessor executionContextAccessor,
        ITenantMessageAuthorizer authorizer,
        IServiceProvider serviceProvider)
    {
        _tenantStore = tenantStore;
        _executionContextAccessor = executionContextAccessor;
        _authorizer = authorizer;
        _serviceProvider = serviceProvider;
    }

    public TenantMessageContextInitializer(
        IMessageContextAccessor messageContextAccessor,
        ITenantStore tenantStore,
        ITenantExecutionContextAccessor executionContextAccessor,
        ITenantMessageAuthorizer authorizer,
        IServiceProvider serviceProvider)
        : this(tenantStore, executionContextAccessor, authorizer, serviceProvider)
    {
        _messageContextAccessor = messageContextAccessor;
    }

    public Task<IAsyncDisposable> BeginAsync(CancellationToken cancellationToken = default)
    {
        var message = _messageContextAccessor?.Current?.Message
            ?? throw new TenantResolutionException("A current message is required to establish tenant context.");

        return BeginAsync(message, cancellationToken);
    }

    public async Task<IAsyncDisposable> BeginAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var tenantId = ReadRequiredGuid(message, HeaderConstants.TenantId, "tenant");
        var tenant = await _tenantStore.FindByIdAsync(tenantId).ConfigureAwait(false)
            ?? throw new TenantResolutionException("The message references an unknown tenant.");
        if (tenant.Status != TenantStatus.Active)
            throw new TenantResolutionException("The message tenant is not active.");

        var validator = _serviceProvider.GetService<ITenantValidator>();
        if (validator is not null && !await validator.IsValidAsync(tenant.Id, cancellationToken).ConfigureAwait(false))
            throw new TenantResolutionException("The message tenant failed validation.");
        if (!await _authorizer.IsAuthorizedAsync(tenant, message, cancellationToken).ConfigureAwait(false))
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
        _executionContextAccessor.Initialize(context);
        return new TenantMessageExecutionScope(_executionContextAccessor);
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
            if (!_disposed)
            {
                accessor.Clear();
                _disposed = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}
