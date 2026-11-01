using Franz.Common.Business.Domain;
using Franz.Common.Business.Events;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Messages;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MongoDB.Repositories.Implementations;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;

namespace Franz.Common.MongoDB.Tests;

public sealed class MongoTenantAggregateRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private IMongoDatabase _database = default!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _database = client.GetDatabase($"franz_tenant_events_{Guid.NewGuid():N}");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task EventStore_ReplaysAndDeletesOnlyEventsOwnedByTheExecutionTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var accessor = new TestTenantExecutionContextAccessor();
        var repository = new MongoAggregateRepository<TestAggregate, TestDomainEvent>(
            _database,
            Substitute.For<IDispatcher>(),
            accessor);

        await repository.CreateTenantIndexesAsync();

        accessor.Initialize(CreateTenantContext(tenantA));
        var aggregateA = new TestAggregate(aggregateId);
        aggregateA.Rename("tenant-a");
        await repository.SaveAsync(aggregateA);

        accessor.Clear();
        accessor.Initialize(CreateTenantContext(tenantB));
        var aggregateB = new TestAggregate(aggregateId);
        aggregateB.Rename("tenant-b");
        await repository.SaveAsync(aggregateB);

        var loadedB = await repository.GetByIdAsync(aggregateId);
        Assert.Equal("tenant-b", loadedB!.Name);
        Assert.Equal(aggregateId, loadedB.Id);

        accessor.Clear();
        accessor.Initialize(CreateTenantContext(tenantA));
        var loadedA = await repository.GetByIdAsync(aggregateId);
        Assert.Equal("tenant-a", loadedA!.Name);
        Assert.Equal(aggregateId, loadedA.Id);

        await repository.DeleteAsync(aggregateId);
        await Assert.ThrowsAsync<NotFoundException>(() => repository.GetByIdAsync(aggregateId));

        accessor.Clear();
        accessor.Initialize(CreateTenantContext(tenantB));
        var stillPresent = await repository.GetByIdAsync(aggregateId);
        Assert.Equal("tenant-b", stillPresent!.Name);
    }

    [Fact]
    public async Task EventStore_FailsClosedWhenTenantExecutionContextIsMissing()
    {
        var repository = new MongoAggregateRepository<TestAggregate, TestDomainEvent>(
            _database,
            Substitute.For<IDispatcher>(),
            new TestTenantExecutionContextAccessor());

        await Assert.ThrowsAsync<TenantIsolationException>(
            () => repository.GetByIdAsync(Guid.NewGuid()));
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

    private sealed class TestAggregate : AggregateRoot<TestDomainEvent>
    {
        public string Name { get; private set; } = string.Empty;

        public TestAggregate()
        {
            RegisterHandlers();
        }

        public TestAggregate(Guid id) : base(id)
        {
            RegisterHandlers();
        }

        private void RegisterHandlers()
            => Register<TestDomainEvent>(domainEvent => Name = domainEvent.Name);

        public void Rename(string name)
        {
            RaiseEvent(new TestDomainEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Id,
                nameof(TestAggregate),
                typeof(TestDomainEvent).AssemblyQualifiedName!,
                DateTimeOffset.UtcNow,
                name));
        }
    }

    private sealed record TestDomainEvent(
        Guid EventId,
        Guid? CorrelationId,
        Guid? AggregateId,
        string AggregateType,
        string EventType,
        DateTimeOffset OccurredOn,
        string Name) : IDomainEvent;
}
