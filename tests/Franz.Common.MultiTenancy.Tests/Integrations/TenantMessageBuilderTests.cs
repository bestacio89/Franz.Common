using Franz.Common.Headers;
using Franz.Common.Messaging;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.MultiTenancy;
using Franz.Common.Messaging.MultiTenancy.Accessors;
using Franz.Common.Messaging.MultiTenancy.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy.Tests.Integrations;

public sealed class TenantMessageBuilderTests
{
    [Fact]
    public void AddMessagingMultitenancyContext_RegistersTenantMetadataBuilders()
    {
        var services = new ServiceCollection();
        services.AddMessagingMultitenancyContext();

        using var provider = services.BuildServiceProvider();
        var builders = provider.GetServices<IMessageBuilder>().ToArray();

        Assert.Contains(builders, builder => builder is TenantMessageBuilder);
        Assert.Contains(builders, builder => builder is DomainMessageBuilder);
    }

    [Fact]
    public async Task BuildAsync_PropagatesValidatedTenantAndDomainContext()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var domainId = Guid.NewGuid();
        var accessor = new MessagingTenantExecutionContextAccessor();
        accessor.Initialize(new TenantExecutionContext(
            tenant,
            domainId,
            TenantResolutionSource.Header,
            DateTimeOffset.UtcNow));
        var builder = new TenantMessageBuilder(accessor);
        var message = new Message("{}");

        await builder.BuildAsync(message);

        Assert.Equal(tenant.Id.ToString(), message.Headers[HeaderConstants.TenantId].Single());
        Assert.Equal(domainId.ToString(), message.Headers[HeaderConstants.DomainId].Single());
    }

    [Fact]
    public async Task BuildAsync_RemovesStaleDomainHeaderWhenCurrentContextHasNoDomain()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = new MessagingTenantExecutionContextAccessor();
        accessor.Initialize(new TenantExecutionContext(
            tenant,
            null,
            TenantResolutionSource.Header,
            DateTimeOffset.UtcNow));
        var builder = new TenantMessageBuilder(accessor);
        var message = new Message("{}");
        message.Headers[HeaderConstants.DomainId] = [Guid.NewGuid().ToString()];

        await builder.BuildAsync(message);

        Assert.Equal(tenant.Id.ToString(), message.Headers[HeaderConstants.TenantId].Single());
        Assert.False(message.Headers.ContainsKey(HeaderConstants.DomainId));
    }
}
