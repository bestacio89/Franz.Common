using Franz.Common.Business.Domain;
using Franz.Common.EntityFramework.Auditing;
using Franz.Common.EntityFramework.Configuration;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace Franz.Common.EntityFramework;

public abstract class DbContextBase : DbContext
{
    private readonly IDispatcher _dispatcher;
    private readonly ICurrentUserService? _currentUser;
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;
    private readonly DatabaseOptions? _databaseOptions;
    private readonly bool _multiTenancyEnabled;

    public const string SoftDeletionQueryFilterName = "Franz.Common.SoftDeletion";
    public const string TenantIsolationQueryFilterName = "Franz.Common.TenantIsolation";

    internal bool MultiTenancyEnabledForModel => _multiTenancyEnabled;
    internal DatabaseIsolationMode? DatabaseIsolationModeForModel => _databaseOptions?.IsolationMode;

    protected DbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        ICurrentUserService? currentUser = null,
        ITenantExecutionContextAccessor? tenantContextAccessor = null)
        : this(options, dispatcher, currentUser, tenantContextAccessor, databaseOptions: null, multiTenancyOptions: null)
    {
    }

    /// <summary>
    /// Creates a context with database and multi-tenancy settings available for model validation.
    /// Multi-tenant applications must explicitly forward both option objects from their composition root.
    /// </summary>
    protected DbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        ICurrentUserService? currentUser,
        ITenantExecutionContextAccessor? tenantContextAccessor,
        IOptions<DatabaseOptions>? databaseOptions)
        : this(options, dispatcher, currentUser, tenantContextAccessor, databaseOptions, multiTenancyOptions: null)
    {
    }

    /// <summary>
    /// Creates a context with explicit multi-tenancy activation and database isolation settings.
    /// </summary>
    protected DbContextBase(
        DbContextOptions options,
        IDispatcher dispatcher,
        ICurrentUserService? currentUser,
        ITenantExecutionContextAccessor? tenantContextAccessor,
        IOptions<DatabaseOptions>? databaseOptions,
        IOptions<MultiTenancyOptions>? multiTenancyOptions) : base(options)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _currentUser = currentUser;
        _tenantContextAccessor = tenantContextAccessor;
        _databaseOptions = databaseOptions?.Value;
        _multiTenancyEnabled = multiTenancyOptions?.Value.Enabled ?? false;
    }

    /// <summary>Current tenant for parameterized filters. An empty value deliberately matches no tenant data.</summary>
    public Guid CurrentTenantId => _tenantContextAccessor?.Current?.TenantId ?? Guid.Empty;

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantIsolation();
        ApplyAuditing();
        ApplyConcurrencyTokens();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantIsolation();
        ApplyAuditing();
        ApplyConcurrencyTokens();
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

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(userId);
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkUpdated(userId);
                    break;
                case EntityState.Deleted:
                    entry.Entity.MarkDeleted(userId);
                    entry.State = EntityState.Modified;
                    break;
            }
        }
    }

    private void ApplyConcurrencyTokens()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyTrackedEntity>())
        {
            var version = entry.Property<long>(nameof(IConcurrencyTrackedEntity.ConcurrencyVersion));

            if (entry.State == EntityState.Added)
            {
                version.CurrentValue = 1L;
            }
            else if (entry.State == EntityState.Modified)
            {
                version.CurrentValue = checked(version.OriginalValue + 1L);
            }
        }
    }

    private static bool ImplementsIEntity(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

    private void ValidateSharedDatabaseEntityClassification(IReadOnlyModel model)
    {
        if (!_multiTenancyEnabled || _databaseOptions?.IsolationMode != DatabaseIsolationMode.SharedDatabase)
            return;

        var violations = model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(ImplementsIEntity)
            .Select(type => new
            {
                Type = type,
                IsTenantOwned = typeof(ITenantEntity).IsAssignableFrom(type),
                IsGlobal = typeof(IGlobalEntity).IsAssignableFrom(type)
            })
            .Where(entity => entity.IsTenantOwned == entity.IsGlobal)
            .Select(entity =>
            {
                var reason = entity.IsTenantOwned
                    ? "implements both ITenantEntity and IGlobalEntity"
                    : "implements neither ITenantEntity nor IGlobalEntity";
                return $"{entity.Type.FullName}: {reason}";
            })
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToArray();

        if (violations.Length > 0)
        {
            throw new InvalidOperationException(
                "SharedDatabase entity classification failed. Every IEntity<TKey> must implement exactly one " +
                "of ITenantEntity or IGlobalEntity. Correct the following model types:\n - " +
                string.Join("\n - ", violations));
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ValidateSharedDatabaseEntityClassification(modelBuilder.Model);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isDomainEntity = ImplementsIEntity(clrType);
            var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var isConcurrencyTrackedEntity = typeof(IConcurrencyTrackedEntity).IsAssignableFrom(clrType);
            if (!isDomainEntity && !isTenantEntity && !isConcurrencyTrackedEntity)
                continue;

            if (isTenantEntity)
                modelBuilder.Entity(clrType).Property<Guid>(nameof(ITenantEntity.TenantId)).IsConcurrencyToken();

            if (isConcurrencyTrackedEntity)
                modelBuilder.Entity(clrType)
                    .Property<long>(nameof(IConcurrencyTrackedEntity.ConcurrencyVersion))
                    .IsConcurrencyToken();

            var parameter = Expression.Parameter(clrType, "entity");
            var entityBuilder = modelBuilder.Entity(clrType);

            if (isDomainEntity)
            {
                var isDeleted = Expression.Property(parameter, nameof(IEntity<object>.IsDeleted));
                var softDeletionFilter = Expression.Lambda(
                    Expression.Equal(isDeleted, Expression.Constant(false)),
                    parameter);
                entityBuilder.HasQueryFilter(SoftDeletionQueryFilterName, softDeletionFilter);
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
                var tenantFilter = Expression.Lambda(Expression.Equal(tenantId, currentTenantId), parameter);
                entityBuilder.HasQueryFilter(TenantIsolationQueryFilterName, tenantFilter);
            }
        }
    }
}
