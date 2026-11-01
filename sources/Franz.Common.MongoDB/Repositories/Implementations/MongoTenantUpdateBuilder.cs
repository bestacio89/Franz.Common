using System.Linq.Expressions;
using Franz.Common.MultiTenancy;
using Franz.Common.MongoDB.Repositories.Contracts;
using MongoDB.Driver;

namespace Franz.Common.MongoDB.Repositories.Implementations;

/// <summary>
/// Builds tenant-safe MongoDB update definitions. Tenant ownership is immutable
/// and cannot be assigned or unset through this builder.
/// </summary>
public sealed class MongoTenantUpdateBuilder<TEntity>
    where TEntity : class, IMongoTenantEntity
{
    private readonly List<UpdateDefinition<TEntity>> _updates = [];

    internal MongoTenantUpdateBuilder()
    {
    }

    public MongoTenantUpdateBuilder<TEntity> Set<TField>(
        Expression<Func<TEntity, TField>> field,
        TField value)
    {
        EnsureMutableField(field);
        _updates.Add(Builders<TEntity>.Update.Set(field, value));
        return this;
    }

    public MongoTenantUpdateBuilder<TEntity> Increment<TField>(
        Expression<Func<TEntity, TField>> field,
        TField amount)
    {
        EnsureMutableField(field);
        _updates.Add(Builders<TEntity>.Update.Inc(field, amount));
        return this;
    }

    public MongoTenantUpdateBuilder<TEntity> Unset(
        Expression<Func<TEntity, object?>> field)
    {
        EnsureMutableField(field);
        _updates.Add(Builders<TEntity>.Update.Unset(field));
        return this;
    }

    internal UpdateDefinition<TEntity> Build()
    {
        if (_updates.Count == 0)
            throw new InvalidOperationException("A MongoDB tenant update must contain at least one operation.");

        return Builders<TEntity>.Update.Combine(_updates);
    }

    private static void EnsureMutableField<TField>(Expression<Func<TEntity, TField>> field)
    {
        ArgumentNullException.ThrowIfNull(field);

        Expression? current = field.Body;
        while (current is UnaryExpression unary)
            current = unary.Operand;

        if (current is not MemberExpression member)
            throw new ArgumentException("MongoDB tenant updates must target entity properties.", nameof(field));

        while (member is not null)
        {
            if (string.Equals(member.Member.Name, nameof(ITenantEntity.TenantId), StringComparison.Ordinal))
                throw new TenantIsolationException("MongoDB tenant updates cannot modify TenantId.");

            if (member.Expression is not MemberExpression parent)
                break;

            member = parent;
        }
    }
}

/// <summary>A single document update included in a tenant-scoped MongoDB bulk write.</summary>
public sealed record MongoTenantBulkUpdate<TEntity>(
    string Id,
    Action<MongoTenantUpdateBuilder<TEntity>> Configure)
    where TEntity : class, IMongoTenantEntity;
