using System.Linq.Expressions;
using Franz.Common.MultiTenancy;
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

    internal void ApplyTo(UpdateSettersBuilder<TEntity> setters)
    {
        if (_assignments.Count == 0)
            throw new InvalidOperationException("A bulk update must define at least one property assignment.");

        foreach (var assignment in _assignments)
            assignment(setters);
    }

    private static void ValidateProperty<TProperty>(Expression<Func<TEntity, TProperty>> selector)
    {
        if (!typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
            return;

        var body = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } conversion
            ? conversion.Operand
            : selector.Body;

        if (body is not MemberExpression member || member.Expression != selector.Parameters[0])
            throw new ArgumentException("Bulk update assignments must target a direct entity property.", nameof(selector));

        if (member.Member.Name == nameof(ITenantEntity.TenantId))
            throw new TenantIsolationException("Bulk updates cannot change TenantId. Tenant ownership is immutable.");
    }
}
