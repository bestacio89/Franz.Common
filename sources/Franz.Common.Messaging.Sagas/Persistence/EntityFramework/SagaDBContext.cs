#nullable enable

using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// EF DbContext for saga state and the inbox/outbox records that must commit with it.
/// </summary>
public sealed class SagaDbContext : DbContextBase
{
  public DbSet<SagaStateEntity> SagaStates => Set<SagaStateEntity>();

  public SagaDbContext(
      DbContextOptions<SagaDbContext> options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null)
      : base(options, dispatcher, currentUser)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<SagaStateEntity>(entity =>
    {
      entity.HasKey(x => new { x.TenantId, x.SagaId });
      entity.Property(x => x.SagaId).IsRequired();
      entity.Property(x => x.CorrelationId).IsRequired();
      entity.Property(x => x.TenantId).IsRequired().HasMaxLength(128);
      entity.Property(x => x.SagaType).IsRequired().HasMaxLength(500);
      entity.Property(x => x.StateType).IsRequired().HasMaxLength(1000);
      entity.Property(x => x.SerializedState).IsRequired();
      entity.Property(x => x.Version).IsConcurrencyToken();
      entity.Property(x => x.Status).IsRequired();
      entity.Property(x => x.CreatedAt).IsRequired();
      entity.Property(x => x.UpdatedAt).IsRequired();
    });

    modelBuilder.Entity<SagaInboxRecord>(entity =>
    {
      entity.ToTable("SagaInbox");
      entity.HasKey(x => x.MessageId);
      entity.Property(x => x.MessageId).ValueGeneratedNever();
      entity.Property(x => x.ProcessedOn).IsRequired();
    });

    modelBuilder.Entity<SagaOutboxRecord>(entity =>
    {
      entity.ToTable("SagaOutbox");
      entity.HasKey(x => x.Id);
      entity.Property(x => x.Id).ValueGeneratedNever();
      entity.Property(x => x.Body).IsRequired();
      entity.Property(x => x.HeadersJson).IsRequired();
      entity.Property(x => x.PropertiesJson).IsRequired();
      entity.Property(x => x.CreatedOn).IsRequired();
      entity.HasIndex(x => new { x.SentOn, x.IsDeadLetter, x.NextAttemptAt, x.LeaseExpiresAt, x.Id });
    });
  }
}
