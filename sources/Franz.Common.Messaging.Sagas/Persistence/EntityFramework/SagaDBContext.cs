#nullable enable

using Franz.Common.EntityFramework;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Microsoft.EntityFrameworkCore;
using System;

namespace Franz.Common.Messaging.Sagas.Persistence.EntityFramework;

/// <summary>
/// EF DbContext for Saga persistence.
/// Inherits from Franz DbContextBase to include:
/// - Auditing (CreatedBy, UpdatedBy)
/// - Soft deletion
/// - Query filters
/// - Domain event dispatch
/// </summary>
public sealed class SagaDbContext : DbContextBase
{
  public DbSet<SagaStateEntity> SagaStates => Set<SagaStateEntity>();

  public SagaDbContext(
      DbContextOptions<SagaDbContext> options,
      IDispatcher dispatcher,
      ICurrentUserService? currentUser = null
  ) : base(options, dispatcher, currentUser)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<SagaStateEntity>(entity =>
    {
      entity.HasKey(x => x.SagaId);

      entity.Property(x => x.SagaId)
            .IsRequired()
            .HasMaxLength(200);

      entity.Property(x => x.SagaType)
            .IsRequired()
            .HasMaxLength(500);

      entity.Property(x => x.SerializedState)
            .IsRequired();

      // Sagas do NOT use soft-delete by default
      // because their lifecycle is controlled by Saga logic.
      // If needed, this can be changed.
    });
  }
}
