using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NSubstitute;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class MigrationQualificationTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private const string InitialMigration = "20261101000100_InitialPersistenceQualification";
    private const string ExpandMigration = "20261101000200_AddDescriptionToPersistenceQualification";
    private const string TableName = "provider_migration_qualification";
    private const string HistoryTable = "__EFMigrationsHistory_Phase5";

    [Fact]
    public async Task Migrations_UpgradeRollbackAndPreserveExistingData()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(
                database.Container.GetConnectionString(),
                provider => provider
                    .MigrationsAssembly(typeof(MigrationQualificationTests).Assembly.GetName().Name)
                    .MigrationsHistoryTable(HistoryTable))
            .Options;

        await using var context = new TestDbContext(options, Substitute.For<IDispatcher>());
        var migrator = context.GetService<IMigrator>();

        try
        {
            var script = migrator.GenerateScript();
            Assert.Contains("CREATE TABLE", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ALTER TABLE", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(TableName, script, StringComparison.OrdinalIgnoreCase);

            await migrator.MigrateAsync(InitialMigration);

            var id = Guid.NewGuid().ToString("D");
            const string originalName = "existing-row-before-upgrade";
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"provider_migration_qualification\" (\"Id\", \"Name\") VALUES ({id}, {originalName})");

            await migrator.MigrateAsync(ExpandMigration);
            Assert.Equal(1, await GetDescriptionColumnCountAsync(context));

            Assert.Equal(originalName, await ReadNameAsync(context, id));

            await migrator.MigrateAsync(InitialMigration);
            Assert.Equal(new[] { InitialMigration }, (await context.Database.GetAppliedMigrationsAsync()).ToArray());
            Assert.Equal(0, await GetDescriptionColumnCountAsync(context));
            Assert.Equal(originalName, await ReadNameAsync(context, id));

            await migrator.MigrateAsync(ExpandMigration);
            Assert.Equal(1, await GetDescriptionColumnCountAsync(context));
            Assert.Equal(originalName, await ReadNameAsync(context, id));
        }
        finally
        {
            await migrator.MigrateAsync("0");
        }
    }

    private static Task<int> GetDescriptionColumnCountAsync(TestDbContext context)
        => context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS \"Value\" FROM information_schema.columns " +
            "WHERE table_schema = current_schema() AND table_name = 'provider_migration_qualification' " +
            "AND column_name = 'Description'").SingleAsync();

    private static Task<string> ReadNameAsync(TestDbContext context, string id)
        => context.Database.SqlQuery<string>(
            $"SELECT \"Name\" AS \"Value\" FROM \"provider_migration_qualification\" WHERE \"Id\" = {id}")
            .SingleAsync();
}

[DbContext(typeof(TestDbContext))]
[Migration("20261101000100_InitialPersistenceQualification")]
public sealed class InitialPersistenceQualification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "provider_migration_qualification",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 36, nullable: false),
                Name = table.Column<string>(maxLength: 160, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_provider_migration_qualification", entity => entity.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "provider_migration_qualification");
}

[DbContext(typeof(TestDbContext))]
[Migration("20261101000200_AddDescriptionToPersistenceQualification")]
public sealed class AddDescriptionToPersistenceQualification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "provider_migration_qualification",
            maxLength: 320,
            nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "Description", table: "provider_migration_qualification");
}
