using System.Linq.Expressions;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Core;

public class MappingExpression<TSource, TDestination> : IMappingExpression
{
  private readonly Dictionary<string, string> _memberBindings = new();
  private readonly HashSet<string> _ignored = new();

  private Delegate? _constructor;
  private Func<TDestination, TSource>? _reverseConstructor;
  private bool _isStrict;

  // =========================================================
  // CONTRACT
  // =========================================================
  public Type SourceType => typeof(TSource);
  public Type DestinationType => typeof(TDestination);

  public IReadOnlyDictionary<string, string> MemberBindings => _memberBindings;
  public IReadOnlyCollection<string> IgnoredMembers => _ignored;

  public bool IsStrict => _isStrict;

  public bool HasConstructor => _constructor != null;

  public Delegate? Constructor => _constructor;
  public Delegate? ReverseConstructor => _reverseConstructor;

  // =========================================================
  // FLUENT API
  // =========================================================
  public MappingExpression<TSource, TDestination> Strict()
  {
    _isStrict = true;
    return this;
  }

  public MappingExpression<TSource, TDestination> ForMember(
      Expression<Func<TDestination, object>> dest,
      Expression<Func<TSource, object>> src)
  {
    _memberBindings[GetName(dest)] = GetName(src);
    return this;
  }

  public MappingExpression<TSource, TDestination> Ignore(
      Expression<Func<TDestination, object>> dest)
  {
    _ignored.Add(GetName(dest));
    return this;
  }

  public MappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor)
  {
    _constructor = ctor;
    return this;
  }

  public MappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, IFranzMapper, TDestination> ctor)
  {
    _constructor = ctor;
    return this;
  }

  public MappingExpression<TSource, TDestination> ReverseConstructUsing(Func<TDestination, TSource> ctor)
  {
    _reverseConstructor = ctor;
    return this;
  }

  private static string GetName(LambdaExpression expr)
  {
    return expr.Body switch
    {
      MemberExpression m => m.Member.Name,
      UnaryExpression { Operand: MemberExpression um } => um.Member.Name,
      _ => throw new InvalidOperationException("Invalid expression")
    };
  }
}