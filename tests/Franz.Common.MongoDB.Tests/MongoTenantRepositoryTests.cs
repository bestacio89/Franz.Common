using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MongoDB.Repositories.Implementations;
using Franz.Common.MongoDB.Repositories.Contracts;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Franz.Common.MongoDB.Tests;

public sealed class MongoTenantRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private IMongoDatabase _database = default!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _database = client.GetDatabase($"franz_tenant_tests_{Guid.NewGuid():N}");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task Repository_ConstrainsReadsAndWritesToTheExecutionTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var documentIdA = Guid.NewGuid().ToString("N");
        var documentIdB = Guid.NewGuid().ToString("N");
        var accessor = new TestTenantExecutionContextAccessor();
        var repository = new MongoTenantRepository<MongoTenantDocument>(
            _database, accessor, "tenant_documents");

        await repository.CreateTenantIdentityIndexAsync();

        accessor.Initialize(CreateTenantContext(tenantA));
        await repository.AddAsync(new MongoTenantDocument
        {
            Id = documentIdA,
            Name = "tenant-a"
        });

        accessor.Clear();
        accessor.Initialize(CreateTenantContext(tenantB));
        await repository.AddAsync(new MongoTenantDocument
        {
            Id = documentIdB,
            Name = "tenant-b"
        });

        using var session = await _database.Client.StartSessionAsync();
        var tenantBDocument = await repository.GetByIdAsync(documentIdB, session: session);
        Assert.Equal("tenant-b", tenantBDocument.Name);
        Assert.Equal(tenantB, tenantBDocument.TenantId);

        accessor.Clear();
        accessor.Initialize(CreateTenantContext(tenantA));
        var tenantADocument = await repository.GetByIdAsync(documentIdA);
        Assert.Equal("tenant-a", tenantADocument.Name);
        Assert.Equal(tenantA, tenantADocument.TenantId);

        await Assert.ThrowsAsync<Franz.Common.Errors.NotFoundException>(
            () => repository.GetByIdAsync(documentIdB));

        var atomicUpdate = await repository.UpdateOneAsync(
            documentIdA,
            update => update.Set(document => document.Name, "tenant-a-atomic")
                .Increment(document => document.Counter, 1));
        Assert.Equal("tenant-a-atomic", atomicUpdate.Name);
        Assert.Equal(1, atomicUpdate.Counter);

        var bulkResult = await repository.BulkUpdateAsync(
            new[]
            {
                new MongoTenantBulkUpdate<MongoTenantDocument>(
                    documentIdA,
                    update => update.Set(document => document.Name, "tenant-a-bulk"))
            });
        Assert.Equal(1, bulkResult.ModifiedCount);

        var manyResult = await repository.UpdateManyAsync(
            document => document.Name == "tenant-a-bulk",
            update => update.Set(document => document.Name, "tenant-a-many"));
        Assert.Equal(1, manyResult.ModifiedCount);

        await Assert.ThrowsAsync<TenantIsolationException>(() =>
            repository.UpdateOneAsync(
                documentIdA,
                update => update.Set(document => document.TenantId, Guid.NewGuid())));
    }

    [Fact]
    public async Task Repository_FailsClosedWithoutTenantContext()
    {
        var accessor = new TestTenantExecutionContextAccessor();
        var repository = new MongoTenantRepository<MongoTenantDocument>(
            _database, accessor, "unscoped_documents");

        await Assert.ThrowsAsync<TenantIsolationException>(
            () => repository.GetAllAsync());

        await Assert.ThrowsAsync<TenantIsolationException>(
            () => repository.AddAsync(new MongoTenantDocument { Id = "unscoped" }));
    }

    [Fact]
    public async Task Repository_RejectsForeignTenantOwnershipClaims()
    {
        var accessor = new TestTenantExecutionContextAccessor();
        var repository = new MongoTenantRepository<MongoTenantDocument>(
            _database, accessor, "ownership_documents");
        var tenantId = Guid.NewGuid();

        accessor.Initialize(CreateTenantContext(tenantId));

        await Assert.ThrowsAsync<TenantIsolationException>(() =>
            repository.AddAsync(new MongoTenantDocument
            {
                Id = "foreign",
                TenantId = Guid.NewGuid()
            }));

        await Assert.ThrowsAsync<TenantIsolationException>(() =>
            repository.UpdateAsync(new MongoTenantDocument
            {
                Id = "foreign",
                TenantId = Guid.NewGuid()
            }));
    }

    private static TenantExecutionContext CreateTenantContext(Guid tenantId)
        => new(
            new TenantInfo(tenantId, tenantId.ToString("D")),
            domainId: null,
            TenantResolutionSource.Unknown,
            DateTimeOffset.UtcNow);

    private sealed class TestTenantExecutionContextAccessor : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; }

        public void Initialize(TenantExecutionContext context)
        {
            if (Current is not null)
                throw new TenantIsolationException("Tenant context is already initialized.");
            Current = context;
        }

        public void Clear() => Current = null;
    }

}

public sealed class MongoTenantDocument : IMongoTenantEntity
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Counter { get; set; }
}
