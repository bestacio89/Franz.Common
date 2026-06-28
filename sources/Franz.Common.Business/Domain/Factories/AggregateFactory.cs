using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Franz.Common.Business.Domain.IdGenerators;
using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.Domain.Factories;

/// <summary>
/// High-performance factory for aggregate roots that require a single-parameter
/// constructor accepting a <see cref="Guid"/> ID.
/// The constructor delegate is compiled once per closed generic type and cached
/// for the lifetime of the application.
/// </summary>
public sealed class AggregateFactory<TAggregate, TEvent> : IAggregateFactory<TAggregate>
    where TAggregate : AggregateRoot<TEvent>
    where TEvent : IEvent
{
  private static readonly Func<Guid, TAggregate> _activator;

  static AggregateFactory()
  {
    var aggregateType = typeof(TAggregate);

    var ctor = aggregateType.GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
        binder: null,
        types: new[] { typeof(Guid) },
        modifiers: null);

    if (ctor is null)
      throw new TypeInitializationException(
          typeof(AggregateFactory<TAggregate, TEvent>).FullName,
          new InvalidOperationException(
              $"{aggregateType.Name} must define a constructor that accepts a single Guid parameter. " +
              $"Add: protected {aggregateType.Name}(Guid id) {{ }}"));

    var idParam = Expression.Parameter(typeof(Guid), "id");
    _activator = Expression.Lambda<Func<Guid, TAggregate>>(
                      Expression.New(ctor, idParam), idParam)
                  .Compile();
  }

  private readonly IIdGenerator<Guid> _idGenerator;

  public AggregateFactory(IIdGenerator<Guid> idGenerator)
  {
    ArgumentNullException.ThrowIfNull(idGenerator);
    _idGenerator = idGenerator;
  }

  /// <summary>
  /// Creates a new <typeparamref name="TAggregate"/> initialised with a
  /// freshly generated ID. The aggregate is valid from construction —
  /// no inconsistent intermediate state is possible.
  /// </summary>
  public TAggregate Create() => _activator(_idGenerator.Create());

  /// <summary>
  /// Triggers the static constructor eagerly so that misconfigured aggregate
  /// types are caught at DI registration time rather than on first use.
  /// Call this from your service-registration extension.
  /// </summary>
  public static void Validate() =>
      RuntimeHelpers.RunClassConstructor(typeof(AggregateFactory<TAggregate, TEvent>).TypeHandle);
}