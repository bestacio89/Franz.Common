using Franz.Common.EntityFramework.MySql.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microting.EntityFrameworkCore.MySql.Infrastructure;

namespace Franz.Common.EntityFramework.MySql.Tests.Migrations;

[Collection(MySqlCollection.Name)]
public sealed class ProviderMigrationTests(MySqlFixture database)
{
  private const string InitialMigration = "202610260001_InitialProviderQualification";
  private const string ExpandMigration = "202610260002_AddDescriptionToProviderQualification";
  private const string TableName = "provider_migration_qualification";

  private DbContextOptions<Franz.Common.EntityFramework.MySql.Tests.TestDbContext> CreateOptions()
  {
    var options = new DbContextOptionsBuilder<Franz.Common.EntityFramework.MySql.Tests.TestDbContext>()
        .UseMySql(
            database.ConnectionString,
            ServerVersion.Create(new Version(8, 4, 0), ServerType.MySql),
            providerOptions => providerOptions
                .MigrationsAssembly(typeof(ProviderMigrationTests).Assembly.GetName().Name)
                .MigrationsHistoryTable("__EFMigrationsHistory_MicrotingQualification"))
        .Options;

    return options;
  }

  [Fact]
  public async Task Provider_AppliesIncrementalMigrationsAndPreservesExistingData()
  {
    await using var context = new Franz.Common.EntityFramework.MySql.Tests.TestDbContext(
        CreateOptions(),
        NSubstitute.Substitute.For<Franz.Common.Mediator.Dispatchers.IDispatcher>());

    var migrator = context.GetService<IMigrator>();
    var generatedScript = migrator.GenerateScript();

    Assert.Contains("CREATE TABLE", generatedScript, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("ALTER TABLE", generatedScript, StringComparison.OrdinalIgnoreCase);
    Assert.Contains(TableName, generatedScript, StringComparison.OrdinalIgnoreCase);

    await migrator.MigrateAsync(InitialMigration);

    var id = Guid.NewGuid().ToString("D");
    const string originalName = "existing-row-before-upgrade";
    await context.Database.ExecuteSqlInterpolatedAsync(
        $"INSERT INTO provider_migration_qualification (Id, Name) VALUES ({id}, {originalName})");

    await migrator.MigrateAsync(ExpandMigration);

    var preservedName = await context.Database.SqlQueryRaw<string>(
        $"SELECT Name AS Value FROM {TableName} WHERE Id = '{id}'").SingleAsync();
    Assert.Equal(originalName, preservedName);

    var descriptionColumnCount = await context.Database.SqlQueryRaw<int>(
        $"SELECT COUNT(*) AS Value FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{TableName}' AND column_name = 'Description'")
        .SingleAsync();
    Assert.Equal(1, descriptionColumnCount);

    var appliedMigrations = await context.Database.SqlQueryRaw<string>(
        "SELECT MigrationId AS Value FROM __EFMigrationsHistory_MicrotingQualification ORDER BY MigrationId")
        .ToListAsync();
    Assert.Equal([InitialMigration, ExpandMigration], appliedMigrations);
  }
}

[DbContext(typeof(Franz.Common.EntityFramework.MySql.Tests.TestDbContext))]
[Migration("202610260001_InitialProviderQualification")]
public sealed class InitialProviderQualification : Migration
{
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.CreateTable(
        name: "provider_migration_qualification",
        columns: table => new
        {
          Id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
          Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_provider_migration_qualification", x => x.Id));
  }

  protected override void Down(MigrationBuilder migrationBuilder) =>
      migrationBuilder.DropTable(name: "provider_migration_qualification");
}

[DbContext(typeof(Franz.Common.EntityFramework.MySql.Tests.TestDbContext))]
[Migration("202610260002_AddDescriptionToProviderQualification")]
public sealed class AddDescriptionToProviderQualification : Migration
{
  protected override void Up(MigrationBuilder migrationBuilder) =>
      migrationBuilder.AddColumn<string>(
          name: "Description",
          table: "provider_migration_qualification",
          type: "varchar(320)",
          maxLength: 320,
          nullable: true);

  protected override void Down(MigrationBuilder migrationBuilder) =>
      migrationBuilder.DropColumn(name: "Description", table: "provider_migration_qualification");
}
