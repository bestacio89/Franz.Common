using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Xunit;

namespace Franz.Common.MultiTenancy.Tests.Persistence;

public sealed class DatabaseTargetResolverTests
{
    [Fact]
    public void SharedDatabase_UsesConcreteConfiguredName()
    {
        var options = new DatabaseOptions { DatabaseName = "application", IsolationMode = DatabaseIsolationMode.SharedDatabase };
        Assert.Equal("application", DatabaseTargetResolver.ResolveDatabaseName(options, null, null));
    }

    [Fact]
    public void SharedDatabase_RejectsRoutingToken()
    {
        var options = new DatabaseOptions { DatabaseName = "application_{dbName}", IsolationMode = DatabaseIsolationMode.SharedDatabase };
        Assert.Throws<InvalidOperationException>(() => DatabaseTargetResolver.ResolveDatabaseName(options, null, null));
    }

    [Fact]
    public void TenantDatabase_UsesTenantIdAndIgnoresDomainId()
    {
        var tenantId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var context = new TenantExecutionContext(new TenantInfo(tenantId, "tenant"), domainId, TenantResolutionSource.Unknown, DateTimeOffset.UtcNow);
        var options = new DatabaseOptions { DatabaseName = "app_{dbName}", IsolationMode = DatabaseIsolationMode.TenantDatabase };
        Assert.Equal($"app_{tenantId:D}", DatabaseTargetResolver.ResolveDatabaseName(options, new StubTenantAccessor(context), null));
    }

    [Fact]
    public void TenantDatabase_FailsClosedWhenTenantContextIsMissingEvenIfDomainExists()
    {
        var options = new DatabaseOptions { DatabaseName = "app_{dbName}", IsolationMode = DatabaseIsolationMode.TenantDatabase };
        Assert.Throws<TenantIsolationException>(() => DatabaseTargetResolver.ResolveDatabaseName(options, null, null));
    }

    [Fact]
    public void DomainDatabase_RequiresExplicitModeAndDomainContext()
    {
        var domainId = Guid.NewGuid();
        var options = new DatabaseOptions { DatabaseName = "app_{dbName}", IsolationMode = DatabaseIsolationMode.DomainDatabase };
        Assert.Equal($"app_{domainId:D}", DatabaseTargetResolver.ResolveDatabaseName(options, null, new StubDomainAccessor(domainId)));
        Assert.Throws<InvalidOperationException>(() => DatabaseTargetResolver.ResolveDatabaseName(options, null, null));
    }

    private sealed class StubTenantAccessor(TenantExecutionContext? current) : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; } = current;
        public void Initialize(TenantExecutionContext context) => Current = context;
        public void Clear() => Current = null;
    }

    private sealed class StubDomainAccessor(Guid? domainId) : IDomainContextAccessor
    {
        public Guid? GetCurrentDomainId() => domainId;
    }
}
