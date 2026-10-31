using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework;

public abstract class DbContextBase : DbContext
{
    private readonly IDispatcher _dispatcher;
    private readonly ICurrentUserService? _currentUser;
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;

    protected DbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        ICurrentUserService? currentUser = null,
        ITenantExecutionContextAccessor? tenantContextAccessor = null
    ) : base(options)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _currentUser = currentUser;
        _tenantContextAccessor = tenantContextAccessor;
    }

    /// <summary>Current tenant for parameterized filters. An empty value deliberately matches no tenant data.</summary>
    public Guid CurrentTenantId => _tenantContextAccessor?.Current?.TenantId ?? Guid.Empty;

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantIsolation();
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantIsolation();
        ApplyAuditing();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantIsolation()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            var currentTenantId = CurrentTenantId;
            if (currentTenantId == Guid.Empty)
                throw new TenantIsolationException("A validated tenant execution context is required to persist tenant-owned entities.");

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = currentTenantId;
                else if (entry.Entity.TenantId != currentTenantId)
                    throw new TenantIsolationException("An entity cannot be inserted for a tenant other than the current execution tenant.");
                continue;
            }

            var originalTenantId = entry.Property(nameof(ITenantEntity.TenantId)).OriginalValue;
            if (originalTenantId is not Guid originalId ||
                originalId != currentTenantId ||
                entry.Entity.TenantId != currentTenantId)
                throw new TenantIsolationException("An entity cannot be updated or deleted outside its owning tenant.");
        }
    }

    private void ApplyAuditing()
    {
        var userId = _currentUser?.UserId ?? "system";

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not IEntity<object> && !ImplementsIEntity(entry.Entity.GetType()))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    ((dynamic)entry.Entity).MarkCreated(userId);
                    break;
                case EntityState.Modified:
                    ((dynamic)entry.Entity).MarkUpdated(userId);
                    break;
                case EntityState.Deleted:
                    ((dynamic)entry.Entity).MarkDeleted(userId);
                    entry.State = EntityState.Modified;
                    break;
            }
        }
    }

    private static bool ImplementsIEntity(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isDomainEntity = ImplementsIEntity(clrType);
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
            if (!isDomainEntity && !isTenantEntity)
                continue;

            var parameter = Expression.Parameter(clrType, "entity");
            Expression? filterBody = null;

            if (isDomainEntity)
            {
                var isDeleted = Expression.Property(parameter, nameof(IEntity<object>.IsDeleted));
                filterBody = Expression.Equal(isDeleted, Expression.Constant(false));
            }

            if (isTenantEntity)
            {
                var tenantId = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(Guid) },
                    parameter,
                    Expression.Constant(nameof(ITenantEntity.TenantId)));
                var currentTenantId = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
                var tenantFilter = Expression.Equal(tenantId, currentTenantId);
                filterBody = filterBody is null ? tenantFilter : Expression.AndAlso(filterBody, tenantFilter);
            }

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(filterBody!, parameter));
        }
    }
}
