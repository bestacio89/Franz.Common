using System.Linq.Expressions;
using Franz.Common.Business.Domain;
using Franz.Common.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Franz.Common.EntityFramework.Repositories;

/// <summary>Collects strongly typed assignments for an EF Core set-based update.</summary>
public sealed class BulkUpdateBuilder<TEntity> where TEntity : class
{
    private readonly List<Action<UpdateSettersBuilder<TEntity>>> _assignments = [];

    /// <summary>Adds an assignment whose value is evaluated by the database query.</summary>
    public BulkUpdateBuilder<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        Expression<Func<TEntity, TProperty>> value)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(value);
        ValidateProperty(property);
        _assignments.Add(setters => setters.SetProperty(property, value));
        return this;
    }

    /// <summary>Adds an assignment with a constant value.</summary>
    public BulkUpdateBuilder<TEntity> SetProperty<TProperty>(
        Expression<Func<TEntity, TProperty>> property,
        TProperty value)
    {
        ArgumentNullException.ThrowIfNull(property);
        ValidateProperty(property);
        _assignments.Add(setters => setters.SetProperty(property, value));
        return this;
    }

    internal bool HasAssignments => _assignments.Count > 0;

    internal void AddConcurrencyVersionIncrement()
    {
        if (!typeof(IConcurrencyTrackedEntity).IsAssignableFrom(typeof(TEntity)))
            return;

        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var currentVersion = Expression.Call(
            typeof(EF),
            nameof(EF.Property),
            new[] { typeof(long) },
            entity,
            Expression.Constant(nameof(IConcurrencyTrackedEntity.ConcurrencyVersion)));
        var nextVersion = Expression.Add(currentVersion, Expression.Constant(1L));

        var propertySelector = Expression.Lambda<Func<TEntity, long>>(currentVersion, entity);
        var valueSelector = Expression.Lambda<Func<TEntity, long>>(nextVersion, entity);
        _assignments.Add(setters => setters.SetProperty(propertySelector, valueSelector));
    }

    internal void ApplyTo(UpdateSettersBuilder<TEntity> setters)
    {
        if (_assignments.Count == 0)
            throw new InvalidOperationException("A bulk update must define at least one property assignment.");

        foreach (var assignment in _assignments)
            assignment(setters);
    }

    private static void ValidateProperty<TProperty>(Expression<Func<TEntity, TProperty>> selector)
    {
        var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity));
        var isConcurrencyTrackedEntity = typeof(IConcurrencyTrackedEntity).IsAssignableFrom(typeof(TEntity));

        var body = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } conversion
            ? conversion.Operand
            : selector.Body;

        if (isConcurrencyTrackedEntity &&
            body is MethodCallExpression propertyCall &&
            propertyCall.Method.DeclaringType == typeof(EF) &&
            propertyCall.Method.Name == nameof(EF.Property) &&
            propertyCall.Arguments.Count == 2 &&
            propertyCall.Arguments[1] is ConstantExpression { Value: string propertyName } &&
            propertyName == nameof(IConcurrencyTrackedEntity.ConcurrencyVersion))
            throw new InvalidOperationException(
                "Bulk updates cannot assign ConcurrencyVersion directly; the framework advances it automatically.");

        if (body is not MemberExpression member || member.Expression != selector.Parameters[0])
        {
            if (isTenantEntity)
                throw new ArgumentException("Bulk update assignments must target a direct entity property.", nameof(selector));

            return;
        }

        if (isTenantEntity && member.Member.Name == nameof(ITenantEntity.TenantId))
            throw new TenantIsolationException("Bulk updates cannot change TenantId. Tenant ownership is immutable.");

        if (isConcurrencyTrackedEntity &&
            member.Member.Name == nameof(IConcurrencyTrackedEntity.ConcurrencyVersion))
            throw new InvalidOperationException(
                "Bulk updates cannot assign ConcurrencyVersion directly; the framework advances it automatically.");
    }
}
