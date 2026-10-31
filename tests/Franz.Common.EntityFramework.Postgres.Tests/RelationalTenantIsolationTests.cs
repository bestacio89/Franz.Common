using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class RelationalTenantIsolationTests(PostgresFixture fixture)
    : RelationalTenantIsolationContract
{
    protected override async Task WithContextAsync(Guid? tenantId, Func<DbContext, Task> action)
    {
        using var provider = (ServiceProvider)fixture.BuildServiceProvider(
            extraRegistrations: services => services.AddSingleton<ITenantExecutionContextAccessor>(
                TenantExecutionContextAccessor.ForTenant(tenantId)));
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await action(context);
    }
}
