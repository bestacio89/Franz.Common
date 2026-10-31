using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.EntityFramework.MariaDB.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Franz.Common.EntityFramework.MariaDB.Tests.Persistence;

[Collection(MariaDbCollection.Name)]
public sealed class RelationalTenantIsolationTests(MariaDbFixture fixture)
    : MariaDbTestBase(fixture), RelationalTenantIsolationContract
{
    protected override async Task WithContextAsync(Guid? tenantId, Func<DbContext, Task> action)
    {
        await using var context = CreateContext(tenantId);
        await action(context);
    }
}
