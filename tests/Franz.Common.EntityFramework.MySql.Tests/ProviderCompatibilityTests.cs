using Franz.Common.EntityFramework.MySql.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit.Abstractions;

namespace Franz.Common.EntityFramework.MySql.Tests;

[Collection(MySqlCollection.Name)]
public sealed class ProviderCompatibilityTests(MySqlFixture database, ITestOutputHelper output)
    : MySqlTestBase(database)
{
  [Fact]
  public async Task MySql84_ReportsExpectedServerAndSupportsRelationalSchemaOperations()
  {
    await using var context = CreateContext();

    Assert.Equal("8.4.0", (await context.Database.SqlQueryRaw<string>(
        "SELECT VERSION() AS Value").SingleAsync()).Split('-', '+')[0][..5]);

    await context.Database.EnsureCreatedAsync();

    var entityType = context.Model.FindEntityType(typeof(TestEntity));
    Assert.NotNull(entityType);
    Assert.Equal("provider_test_entities", entityType!.GetTableName());

    var generatedSql = context.Entities
        .Where(entity => entity.Name.StartsWith("compat-"))
        .ToQueryString();

    output.WriteLine(generatedSql);
    Assert.Contains("provider_test_entities", generatedSql, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("IsDeleted", generatedSql, StringComparison.OrdinalIgnoreCase);
  }
}
