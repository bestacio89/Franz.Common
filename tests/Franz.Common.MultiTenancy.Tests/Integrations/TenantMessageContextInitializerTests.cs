using Franz.Common.Headers;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantMessageContextInitializerTests
{
    [Fact]
    public async Task BeginAsync_ValidatesAndScopesTenantContext()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var message = CreateMessage(tenant.Id);
        var executionAccessor = new MessagingTenantExecutionContextAccessor();
        var initializer = CreateInitializer(tenant, message, executionAccessor, authorized: true);

        await using (await initializer.BeginAsync())
        {
            Assert.Equal(tenant.Id, executionAccessor.Current?.TenantId);
        }

        Assert.Null(executionAccessor.Current);
    }

    [Fact]
    public async Task BeginAsync_RejectsUnauthorizedProducer()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var executionAccessor = new MessagingTenantExecutionContextAccessor();
        var initializer = CreateInitializer(tenant, CreateMessage(tenant.Id), executionAccessor, authorized: false);

        await Assert.ThrowsAsync<TenantResolutionException>(() => initializer.BeginAsync());
        Assert.Null(executionAccessor.Current);
    }

    private static TenantMessageContextInitializer CreateInitializer(
        TenantInfo tenant,
        Message message,
        ITenantExecutionContextAccessor executionAccessor,
        bool authorized)
    {
        var store = new InMemoryTenantStore([tenant]);
        var messageAccessor = new StubMessageContextAccessor(new MessageContext(message));
        var authorizer = new StubAuthorizer(authorized);
        return new TenantMessageContextInitializer(messageAccessor, store, executionAccessor, authorizer,
            new ServiceCollection().BuildServiceProvider());
    }

    private static Message CreateMessage(Guid tenantId)
    {
        var message = new Message();
        message.Headers[HeaderConstants.TenantId] = [tenantId.ToString()];
        return message;
    }

    private sealed class StubMessageContextAccessor(IMessageContext current) : IMessageContextAccessor
    {
        public IMessageContext? Current { get; } = current;
    }

    private sealed class StubAuthorizer(bool authorized) : ITenantMessageAuthorizer
    {
        public Task<bool> IsAuthorizedAsync(TenantInfo tenant, Message message, CancellationToken cancellationToken = default)
            => Task.FromResult(authorized);
    }
}
