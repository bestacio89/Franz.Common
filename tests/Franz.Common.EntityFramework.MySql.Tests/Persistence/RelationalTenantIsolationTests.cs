using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using NSubstitute;
using Xunit;

namespace Franz.Common.EntityFramework.MySql.Tests.Persistence;

[Collection(MySqlCollection.Name)]
public sealed class RelationalTenantIsolationTests(MySqlFixture fixture)
    : RelationalTenantIsolationContract
{
    protected override async Task WithContextAsync(Guid? tenantId, Func<DbContext, Task> action)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseMySql(fixture.ConnectionString,
                ServerVersion.Create(new Version(8, 4, 0), ServerType.MySql))
            .Options;
        await using var context = new TestDbContext(
            options,
            Substitute.For<IDispatcher>(),
            tenantContextAccessor: TenantExecutionContextAccessor.ForTenant(tenantId));
        await action(context);
    }
}
