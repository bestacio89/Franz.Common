using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Franz.Common.Business.Domain.IdGenerators;

namespace Franz.Common.Business.Domain.Factories;

/// <summary>
/// High-performance factory for domain entities that require a single-parameter
/// constructor accepting a <typeparamref name="TKey"/> ID.
/// The constructor delegate is compiled once per closed generic type and cached
/// for the lifetime of the application.
/// </summary>
public sealed class EntityFactory<TKey, TEntity> : IEntityFactory<TKey, TEntity>
    where TEntity : Entity<TKey>
{
  private static readonly Func<TKey, TEntity> _activator;

  static EntityFactory()
  {
    var entityType = typeof(TEntity);
    var keyType = typeof(TKey);

    var ctor = entityType.GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        binder: null,
        types: new[] { keyType },
        modifiers: null);

    if (ctor is null)
      throw new TypeInitializationException(
          typeof(EntityFactory<TKey, TEntity>).FullName,
          new InvalidOperationException(
              $"{entityType.Name} must define a constructor that accepts a single {keyType.Name} parameter. " +
              $"Add: protected {entityType.Name}({keyType.Name} id) {{ }}"));

    var idParam = Expression.Parameter(keyType, "id");
    _activator = Expression.Lambda<Func<TKey, TEntity>>(
                      Expression.New(ctor, idParam), idParam)
                  .Compile();
  }

  private readonly IIdGenerator<TKey> _idGenerator;

  public EntityFactory(IIdGenerator<TKey> idGenerator)
  {
    ArgumentNullException.ThrowIfNull(idGenerator);
    _idGenerator = idGenerator;
  }

  /// <summary>
  /// Creates a new <typeparamref name="TEntity"/> initialised with a
  /// freshly generated ID. The entity is valid from construction —
  /// no inconsistent intermediate state is possible.
  /// </summary>
  public TEntity Create() => _activator(_idGenerator.Create());

  /// <summary>
  /// Triggers the static constructor eagerly so that misconfigured entity
  /// types are caught at DI registration time rather than on first use.
  /// Call this from your service-registration extension.
  /// </summary>
  public static void Validate() =>
      RuntimeHelpers.RunClassConstructor(typeof(EntityFactory<TKey, TEntity>).TypeHandle);
}