using System.Linq.Expressions;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public sealed class MappingExpression<TSource, TDestination> : IMappingExpression
{
  private readonly Dictionary<string, string> _memberBindings = [];
  private readonly HashSet<string> _ignored = [];

  private Delegate? _constructor;
  private Func<TDestination, TSource>? _reverseConstructor;

  private bool _isStrict;


  // =========================================================
  // CONTRACT
  // =========================================================

  public Type SourceType => typeof(TSource);

  public Type DestinationType => typeof(TDestination);

  public IReadOnlyDictionary<string, string> MemberBindings =>
      _memberBindings;

  public IReadOnlyCollection<string> IgnoredMembers =>
      _ignored;

  public bool IsStrict =>
      _isStrict;

  public bool HasConstructor =>
      _constructor != null;

  public Delegate? Constructor =>
      _constructor;

  public Delegate? ReverseConstructor =>
      _reverseConstructor;


  // =========================================================
  // FLUENT API
  // =========================================================

  public MappingExpression<TSource, TDestination> Strict()
  {
    _isStrict = true;

    return this;
  }


  public MappingExpression<TSource, TDestination> ForMember(
      Expression<Func<TDestination, object>> destination,
      Expression<Func<TSource, object>> source)
  {
    ArgumentNullException.ThrowIfNull(destination);
    ArgumentNullException.ThrowIfNull(source);

    var destinationName = GetName(destination);
    var sourceName = GetName(source);

    _memberBindings[destinationName] = sourceName;

    return this;
  }


  public MappingExpression<TSource, TDestination> Ignore(
      Expression<Func<TDestination, object>> destination)
  {
    ArgumentNullException.ThrowIfNull(destination);

    _ignored.Add(GetName(destination));

    return this;
  }


  public MappingExpression<TSource, TDestination> ConstructUsing(
      Func<TSource, TDestination> constructor)
  {
    ArgumentNullException.ThrowIfNull(constructor);

    _constructor = constructor;

    return this;
  }


  public MappingExpression<TSource, TDestination> ConstructUsing(
      Func<TSource, IFranzMapper, TDestination> constructor)
  {
    ArgumentNullException.ThrowIfNull(constructor);

    _constructor = constructor;

    return this;
  }


  public MappingExpression<TSource, TDestination> ReverseConstructUsing(
      Func<TDestination, TSource> constructor)
  {
    ArgumentNullException.ThrowIfNull(constructor);

    _reverseConstructor = constructor;

    return this;
  }


  // =========================================================
  // EXPRESSION PARSING
  // =========================================================

  private static string GetName(LambdaExpression expression)
  {
    Expression body = expression.Body;

    // Handles:
    // x => (object)x.Property
    if (body is UnaryExpression unary &&
        unary.NodeType == ExpressionType.Convert)
    {
      body = unary.Operand;
    }


    if (body is MemberExpression member)
    {
      return member.Member.Name;
    }


    throw new InvalidOperationException(
        $"Invalid Mapping expression '{expression}'. " +
        "Expected a direct member access.");
  }
}