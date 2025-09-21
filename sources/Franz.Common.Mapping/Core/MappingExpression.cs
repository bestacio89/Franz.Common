using System.Linq.Expressions;

namespace Franz.Common.Mapping.Core;

public class MappingExpression<TSource, TDestination>
{
  private readonly Dictionary<string, string> _memberMap = new();
  private readonly HashSet<string> _ignored = new();
  private Func<TSource, TDestination>? _construct;

  // Lambda-based API
  public MappingExpression<TSource, TDestination> ForMember<TMember>(
      Expression<Func<TDestination, TMember>> destination,
      Expression<Func<TSource, TMember>> source)
  {
    var destName = ((MemberExpression)destination.Body).Member.Name;
    var sourceName = ((MemberExpression)source.Body).Member.Name;

    _memberMap[destName] = sourceName;
    return this;
  }

  // String-based API
  public MappingExpression<TSource, TDestination> ForMember(string destinationName, string sourceName)
  {
    _memberMap[destinationName] = sourceName;
    return this;
  }

  public MappingExpression<TSource, TDestination> Ignore(string destinationName)
  {
    _ignored.Add(destinationName);
    return this;
  }

  public MappingExpression<TSource, TDestination> Ignore<TMember>(
      Expression<Func<TDestination, TMember>> destination)
  {
    var destName = ((MemberExpression)destination.Body).Member.Name;
    _ignored.Add(destName);
    return this;
  }

  // Reverse mapping support
  public MappingExpression<TDestination, TSource> ReverseMap()
  {
    var reverse = new MappingExpression<TDestination, TSource>();

    foreach (var ignore in _ignored)
      reverse.Ignore(ignore);

    foreach (var kvp in _memberMap)
      reverse.ForMember(kvp.Value, kvp.Key);

    return reverse;
  }

  public MappingExpression<TSource, TDestination> ConstructUsing(
      Func<TSource, TDestination> factory)
  {
    _construct = factory;
    return this;
  }

  internal Dictionary<string, string> MemberMap => _memberMap;
  internal HashSet<string> Ignored => _ignored;
  internal Func<TSource, TDestination>? Constructor => _construct;
}
