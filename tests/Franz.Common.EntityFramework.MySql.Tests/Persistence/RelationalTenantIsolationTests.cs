using Franz.Common.EntityFramework.ProviderContract.Tests;
using Franz.Common.EntityFramework.MySql.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Franz.Common.EntityFramework.MySql.Tests.Persistence;

[Collection(MySqlCollection.Name)]
public sealed class RelationalTenantIsolationTests(MySqlFixture fixture)
    : MySqlTestBase(fixture), RelationalTenantIsolationContract
{
    protected override async Task WithContextAsync(Guid? tenantId, Func<DbContext, Task> action)
    {
        await using var context = CreateContext(tenantId);
        await action(context);
    }
}
