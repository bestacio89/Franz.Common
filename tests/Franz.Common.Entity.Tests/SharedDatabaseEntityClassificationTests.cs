using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Franz.Common.EntityFramework.Tests;

public sealed class SharedDatabaseEntityClassificationTests
{
    [Fact]
    public void SharedDatabase_AllowsExplicitTenantOwnedAndGlobalEntities()
    {
        using var context = new ClassifiedDbContext(
            CreateOptions<ClassifiedDbContext>(),
            new Mock<IDispatcher>().Object,
            Options.Create(new DatabaseOptions { DatabaseName = "shared", IsolationMode = DatabaseIsolationMode.SharedDatabase }),
            Options.Create(new MultiTenancyOptions { Enabled = true }));

        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(TenantOwnedEntity));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(GlobalEntity));
    }

    [Fact]
    public void SharedDatabase_RejectsUnclassifiedDomainEntityWithActionableDiagnostic()
    {
        using var context = new UnclassifiedDbContext(
            CreateOptions<UnclassifiedDbContext>(),
            new Mock<IDispatcher>().Object,
            Options.Create(new DatabaseOptions { DatabaseName = "shared", IsolationMode = DatabaseIsolationMode.SharedDatabase }),
            Options.Create(new MultiTenancyOptions { Enabled = true }));

        var exception = Assert.Throws<InvalidOperationException>(() => _ = context.Model);

        Assert.Contains(nameof(UnclassifiedEntity), exception.Message);
        Assert.Contains(nameof(ITenantEntity), exception.Message);
        Assert.Contains(nameof(IGlobalEntity), exception.Message);
    }

    [Fact]
    public void SharedDatabase_RejectsEntityMarkedAsBothTenantOwnedAndGlobal()
    {
        using var context = new AmbiguousDbContext(
            CreateOptions<AmbiguousDbContext>(),
            new Mock<IDispatcher>().Object,
            Options.Create(new DatabaseOptions { DatabaseName = "shared", IsolationMode = DatabaseIsolationMode.SharedDatabase }),
            Options.Create(new MultiTenancyOptions { Enabled = true }));

        var exception = Assert.Throws<InvalidOperationException>(() => _ = context.Model);

        Assert.Contains(nameof(AmbiguousEntity), exception.Message);
        Assert.Contains("both", exception.Message);
    }

    [Fact]
    public void NonMultiTenantApplication_SharedDatabaseAllowsUnclassifiedDomainEntities()
    {
        using var context = new UnclassifiedDbContext(
            CreateOptions<UnclassifiedDbContext>(),
            new Mock<IDispatcher>().Object,
            Options.Create(new DatabaseOptions { DatabaseName = "game", IsolationMode = DatabaseIsolationMode.SharedDatabase }),
            Options.Create(new MultiTenancyOptions { Enabled = false }));

        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(UnclassifiedEntity));
    }

    [Fact]
    public void TenantDatabase_DoesNotApplySharedDatabaseClassificationGate()
    {
        using var context = new UnclassifiedDbContext(
            CreateOptions<UnclassifiedDbContext>(),
            new Mock<IDispatcher>().Object,
            Options.Create(new DatabaseOptions { DatabaseName = "tenant_{dbName}", IsolationMode = DatabaseIsolationMode.TenantDatabase }),
            Options.Create(new MultiTenancyOptions { Enabled = true }));

        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(UnclassifiedEntity));
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>() where TContext : DbContext
        => new DbContextOptionsBuilder<TContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;

    private sealed class TenantOwnedEntity : Entity<int>, ITenantEntity
    {
        public Guid TenantId { get; set; }
    }

    private sealed class GlobalEntity : Entity<int>, IGlobalEntity
    {
    }

    private sealed class UnclassifiedEntity : Entity<int>
    {
    }

    private sealed class AmbiguousEntity : Entity<int>, ITenantEntity, IGlobalEntity
    {
        public Guid TenantId { get; set; }
    }

    private sealed class ClassifiedDbContext(
        DbContextOptions<ClassifiedDbContext> options,
        IDispatcher dispatcher,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
        : DbContextBase(options, dispatcher, currentUser: null, tenantContextAccessor: null, databaseOptions: databaseOptions, multiTenancyOptions: multiTenancyOptions)
    {
        public DbSet<TenantOwnedEntity> TenantEntities => Set<TenantOwnedEntity>();
        public DbSet<GlobalEntity> GlobalEntities => Set<GlobalEntity>();
    }

    private sealed class UnclassifiedDbContext(
        DbContextOptions<UnclassifiedDbContext> options,
        IDispatcher dispatcher,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
        : DbContextBase(options, dispatcher, currentUser: null, tenantContextAccessor: null, databaseOptions: databaseOptions, multiTenancyOptions: multiTenancyOptions)
    {
        public DbSet<UnclassifiedEntity> Entities => Set<UnclassifiedEntity>();
    }

    private sealed class AmbiguousDbContext(
        DbContextOptions<AmbiguousDbContext> options,
        IDispatcher dispatcher,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
        : DbContextBase(options, dispatcher, currentUser: null, tenantContextAccessor: null, databaseOptions: databaseOptions, multiTenancyOptions: multiTenancyOptions)
    {
        public DbSet<AmbiguousEntity> Entities => Set<AmbiguousEntity>();
    }
}
