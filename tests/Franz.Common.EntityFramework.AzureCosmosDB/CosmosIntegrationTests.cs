using FluentAssertions;
using Franz.Common.Business.Domain.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.AzureCosmosDB.Repositories;
using Xunit;

namespace Franz.Common.AzureCosmosDB.Tests;

[Collection("CosmosIntegration")]
public sealed class CosmosIntegrationTests : IClassFixture<CosmosFixture>
{
  private readonly CosmosFixture _fixture;

  public CosmosIntegrationTests(CosmosFixture fixture)
  {
    _fixture = fixture;
  }


  [Fact]
  public async Task TenantOwnedDocuments_ArePartitionedAndFilteredByValidatedTenant()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();
    var labelA = $"tenant-a-{Guid.NewGuid():N}";
    var labelB = $"tenant-b-{Guid.NewGuid():N}";

    try
    {
      using (var tenantAScope = testContext.CreateScope())
      {
        var accessor = tenantAScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantA));
        var db = tenantAScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        db.TenantItems.Add(new TenantCosmosEntity { Label = labelA });
        await db.SaveChangesAsync();
      }

      using (var tenantBScope = testContext.CreateScope())
      {
        var accessor = tenantBScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantB));
        var db = tenantBScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        db.TenantItems.Add(new TenantCosmosEntity { Label = labelB });
        await db.SaveChangesAsync();
      }

      using (var tenantAScope = testContext.CreateScope())
      {
        var accessor = tenantAScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantA));
        var db = tenantAScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var visible = await db.TenantItems.AsNoTracking().ToListAsync();

        visible.Should().ContainSingle();
        visible[0].Label.Should().Be(labelA);
        visible[0].TenantId.Should().Be(tenantA);
      }

      using (var unscopedScope = testContext.CreateScope())
      {
        var db = unscopedScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var visible = await db.TenantItems.AsNoTracking().ToListAsync();
        visible.Should().BeEmpty();

        db.TenantItems.Add(new TenantCosmosEntity { Label = "unscoped" });
        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
      }
    }
    finally
    {
      await testContext.CleanUpAsync();
    }
  }

  [Fact]
  public async Task NativeCosmosRepository_ConstrainsPointReadsQueriesAndReadManyToTenantPartition()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();
    var tenantA = Guid.NewGuid();
    var tenantB = Guid.NewGuid();
    var documentId = Guid.NewGuid().ToString("N");

    try
    {
      using (var tenantAScope = testContext.CreateScope())
      {
        var accessor = tenantAScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantA));
        var repository = tenantAScope.ServiceProvider
            .GetRequiredService<CosmosTenantRepository<CosmosSdkTenantDocument>>();

        var created = await repository.CreateAsync(new CosmosSdkTenantDocument
        {
          Id = documentId,
          Label = "tenant-a"
        });
        created.RequestCharge.Should().BeGreaterThan(0);
      }

      using (var tenantBScope = testContext.CreateScope())
      {
        var accessor = tenantBScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantB));
        var repository = tenantBScope.ServiceProvider
            .GetRequiredService<CosmosTenantRepository<CosmosSdkTenantDocument>>();

        await repository.CreateAsync(new CosmosSdkTenantDocument
        {
          Id = documentId,
          Label = "tenant-b"
        });
      }

      using (var tenantAScope = testContext.CreateScope())
      {
        var accessor = tenantAScope.ServiceProvider.GetRequiredService<ITenantExecutionContextAccessor>();
        accessor.Initialize(CreateTenantContext(tenantA));
        var repository = tenantAScope.ServiceProvider
            .GetRequiredService<CosmosTenantRepository<CosmosSdkTenantDocument>>();

        var pointRead = await repository.GetByIdWithMetadataAsync(documentId);
        pointRead.Resource.Label.Should().Be("tenant-a");
        pointRead.Resource.TenantId.Should().Be(tenantA);
        pointRead.RequestCharge.Should().BeGreaterThan(0);
        pointRead.ETag.Should().NotBeNullOrWhiteSpace();

        var query = await repository.QueryWithMetadataAsync(new QueryDefinition("SELECT * FROM c"));
        query.Items.Should().ContainSingle();
        query.Items[0].TenantId.Should().Be(tenantA);
        query.RequestCharge.Should().BeGreaterThan(0);

        var many = await repository.ReadManyWithMetadataAsync(new[] { documentId });
        many.Items.Should().ContainSingle();
        many.Items[0].TenantId.Should().Be(tenantA);

        var replacement = await repository.ReplaceAsync(
            new CosmosSdkTenantDocument
            {
              Id = documentId,
              TenantId = tenantA,
              Label = "tenant-a-updated"
            },
            pointRead.ETag!);
        replacement.ETag.Should().NotBeNullOrWhiteSpace();
        replacement.RequestCharge.Should().BeGreaterThan(0);

        var staleWrite = () => repository.ReplaceAsync(
            new CosmosSdkTenantDocument
            {
              Id = documentId,
              TenantId = tenantA,
              Label = "stale-write"
            },
            pointRead.ETag!);
        var concurrencyException = await Assert.ThrowsAsync<CosmosException>(staleWrite);
        concurrencyException.StatusCode.Should().Be(System.Net.HttpStatusCode.PreconditionFailed);
      }

      using (var unscopedScope = testContext.CreateScope())
      {
        var repository = unscopedScope.ServiceProvider
            .GetRequiredService<CosmosTenantRepository<CosmosSdkTenantDocument>>();

        await Assert.ThrowsAsync<TenantIsolationException>(() =>
            repository.GetByIdAsync(documentId));
      }
    }
    finally
    {
      await testContext.CleanUpAsync();
    }
  }

  private static TenantExecutionContext CreateTenantContext(Guid tenantId)
      => new(
          new TenantInfo(tenantId, tenantId.ToString("D")),
          domainId: null,
          TenantResolutionSource.Unknown,
          DateTimeOffset.UtcNow);

  [Fact]
  public async Task AddAsync_ShouldPersistJsonDocument_WithAuditMetadata()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();

    try
    {
      Guid entityId;
      var beforeSave = DateTimeOffset.UtcNow;

      using (var scope = testContext.CreateScope())
      {
        var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

        var entity = factory.Create();
        entity.Label = $"Cosmos Item {Guid.NewGuid():N}";
        entityId = entity.Id;

        db.Items.Add(entity);
        await db.SaveChangesAsync();
      }

      using (var verifyScope = testContext.CreateScope())
      {
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var saved = await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == entityId);

        saved.Should().NotBeNull();
        saved!.DateCreated.Should().BeCloseTo(beforeSave, TimeSpan.FromSeconds(5));
        saved.CreatedBy.Should().Be("system");
      }
    }
    finally
    {
      await testContext.CleanUpAsync();
    }
  }

  [Fact]
  public async Task SoftDelete_ShouldApplyGlobalFilter()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();

    try
    {
      Guid id;

      using (var scope = testContext.CreateScope())
      {
        var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

        var entity = factory.Create();
        entity.Label = $"Disposable {Guid.NewGuid():N}";

        db.Items.Add(entity);
        await db.SaveChangesAsync();

        id = entity.Id;

        db.Items.Remove(entity);
        await db.SaveChangesAsync();
      }

      using (var verifyScope = testContext.CreateScope())
      {
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

        var filtered = await verifyDb.Items.FirstOrDefaultAsync(x => x.Id == id);
        var raw = await verifyDb.Items
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id);

        filtered.Should().BeNull();
        raw.Should().NotBeNull();
        raw!.IsDeleted.Should().BeTrue();
      }
    }
    finally
    {
      await testContext.CleanUpAsync();
    }
  }

  [Fact]
  public async Task GlobalFilter_ShouldExcludeSoftDeleted()
  {
    var testContext = await _fixture.CreateIsolatedDatabaseContextAsync();

    try
    {
      Guid activeId;
      Guid deletedId;

      using (var scope = testContext.CreateScope())
      {
        var db = scope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IEntityFactory<Guid, CosmosEntity>>();

        var active = factory.Create();
        active.Label = $"Active {Guid.NewGuid():N}";
        activeId = active.Id;

        var deleted = factory.Create();
        deleted.Label = $"Deleted {Guid.NewGuid():N}";
        deletedId = deleted.Id;

        db.Items.AddRange(active, deleted);
        await db.SaveChangesAsync();

        db.Items.Remove(deleted);
        await db.SaveChangesAsync();
      }

      using (var verifyScope = testContext.CreateScope())
      {
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<TestCosmosDbContext>();

        var visible = await verifyDb.Items.ToListAsync();
        visible.Should().ContainSingle(x => x.Id == activeId);

        var all = await verifyDb.Items.IgnoreQueryFilters().ToListAsync();
        all.Should().HaveCount(2);
        all.Should().Contain(x => x.Id == deletedId && x.IsDeleted);
      }
    }
    finally
    {
      await testContext.CleanUpAsync();
    }
  }
}

[CollectionDefinition("CosmosIntegration", DisableParallelization = true)]
public class CosmosIntegrationCollection { }