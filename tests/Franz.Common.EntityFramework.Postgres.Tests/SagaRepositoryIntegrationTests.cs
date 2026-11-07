using FluentAssertions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Messaging.Sagas.Abstractions;
using Franz.Common.Messaging.Sagas.Exceptions;
using Franz.Common.Messaging.Sagas.Persistence;
using Franz.Common.Messaging.Sagas.Persistence.EntityFramework;
using Franz.Common.Messaging.Sagas.Persistence.Serializer;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Franz.Common.EntityFramework.Postgres.Tests;

public sealed class SagaRepositoryIntegrationTests : IClassFixture<PostgresFixture>
{
  private readonly PostgresFixture _fixture;

  public SagaRepositoryIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

  [Fact]
  public async Task Repository_EnforcesTenantScopedPersistenceAndOptimisticConcurrency()
  {
    await using (var schemaContext = CreateContext())
      await schemaContext.Database.EnsureCreatedAsync();

    var sagaId = Guid.NewGuid();
    var tenantA = CreateRecord(sagaId, "tenant-a", "tenant-a");
    var tenantB = CreateRecord(sagaId, "tenant-b", "tenant-b");
    SagaPersistenceRecord createdA;

    await using (var context = CreateContext())
    {
      var repository = CreateRepository(context);
      createdA = await repository.SaveAsync(tenantA, expectedVersion: null);
      await repository.SaveAsync(tenantB, expectedVersion: null);

      var updated = await repository.SaveAsync(
          tenantA with
          {
            CreatedAt = tenantA.CreatedAt.AddDays(30),
            State = new PostgresSagaState { Value = "tenant-a-updated" }
          },
          createdA.Version);

      updated.CreatedAt.Should().Be(tenantA.CreatedAt);
      updated.Version.Should().Be(2);
    }

    await using (var restartContext = CreateContext())
    {
      var repository = CreateRepository(restartContext);
      var loadedA = await repository.LoadAsync(sagaId, "tenant-a", typeof(PostgresSagaState));
      var loadedB = await repository.LoadAsync(sagaId, "tenant-b", typeof(PostgresSagaState));
      var unscoped = await repository.LoadAsync(sagaId, null, typeof(PostgresSagaState));

      loadedA.Should().NotBeNull();
      loadedA!.CreatedAt.Should().Be(tenantA.CreatedAt);
      loadedA.Version.Should().Be(2);
      ((PostgresSagaState)loadedA.State).Value.Should().Be("tenant-a-updated");
      ((PostgresSagaState)loadedB!.State).Value.Should().Be("tenant-b");
      unscoped.Should().BeNull();
    }

    var competingSaga = CreateRecord();
    await using (var createContext = CreateContext())
      await CreateRepository(createContext).SaveAsync(competingSaga, expectedVersion: null);

    await using var writerAContext = CreateContext();
    await using var writerBContext = CreateContext();
    var writerA = CreateRepository(writerAContext);
    var writerB = CreateRepository(writerBContext);

    async Task<bool> TryUpdateAsync(
        EFSagaRepository repository,
        string value)
    {
      try
      {
        await repository.SaveAsync(
            competingSaga with { State = new PostgresSagaState { Value = value } },
            expectedVersion: 1);
        return true;
      }
      catch (SagaConcurrencyException)
      {
        return false;
      }
    }

    var outcomes = await Task.WhenAll(
        TryUpdateAsync(writerA, "writer-a"),
        TryUpdateAsync(writerB, "writer-b"));
    outcomes.Count(x => x).Should().Be(1);

    await using (var deleteContext = CreateContext())
    {
      var repository = CreateRepository(deleteContext);
      await repository.DeleteAsync(sagaId, "tenant-a", expectedVersion: 2);
      (await repository.LoadAsync(sagaId, "tenant-a", typeof(PostgresSagaState))).Should().BeNull();
      (await repository.LoadAsync(sagaId, "tenant-b", typeof(PostgresSagaState))).Should().NotBeNull();
    }
  }

  private SagaDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<SagaDbContext>()
        .UseNpgsql(_fixture.Container.GetConnectionString())
        .Options;
    return new SagaDbContext(options, Substitute.For<IDispatcher>());
  }

  private static EFSagaRepository CreateRepository(SagaDbContext context)
      => new(context, new JsonSagaStateSerializer());

  private static SagaPersistenceRecord CreateRecord(
      Guid? sagaId = null,
      string? tenantId = "tenant-a",
      string value = "initial")
  {
    var now = DateTimeOffset.UtcNow;
    return new SagaPersistenceRecord
    {
      SagaId = sagaId ?? Guid.NewGuid(),
      TenantId = tenantId,
      SagaType = "postgres-contract-saga",
      State = new PostgresSagaState { Value = value },
      CreatedAt = now,
      UpdatedAt = now
    };
  }

  public sealed class PostgresSagaState : ISagaState
  {
    public string? ConcurrencyToken { get; set; }
    public string Value { get; set; } = string.Empty;
  }
}
