using System.Linq.Expressions;

public class MappingExpression<TSource, TDestination>
{
  private readonly Dictionary<string, string> _memberBindings = new();
  private readonly HashSet<string> _ignored = new();

  // source-aware constructor
  internal Func<TSource, TDestination>? Constructor { get; private set; }

  // reverse constructor (optional)
  internal Func<TDestination, TSource>? ReverseConstructor { get; private set; }

  // expose bindings + ignored members
  public IReadOnlyDictionary<string, string> MemberBindings => _memberBindings;
  public IReadOnlyCollection<string> IgnoredMembers => _ignored;

  public MappingExpression<TSource, TDestination> ForMember(
      Expression<Func<TDestination, object>> destMember,
      Expression<Func<TSource, object>> srcMember)
  {
    var destName = GetMemberName(destMember);
    var srcName = GetMemberName(srcMember);

    _memberBindings[destName] = srcName;
    return this;
  }

  public MappingExpression<TSource, TDestination> Ignore(
      Expression<Func<TDestination, object>> destMember)
  {
    var destName = GetMemberName(destMember);
    _ignored.Add(destName);
    return this;
  }

  // Forward constructor
  public MappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor)
  {
    Constructor = ctor;
    return this;
  }

  // Reverse constructor
  public MappingExpression<TSource, TDestination> ReverseConstructUsing(Func<TDestination, TSource> ctor)
  {
    ReverseConstructor = ctor;
    return this;
  }

  private string GetMemberName(LambdaExpression expr)
  {
    if (expr.Body is MemberExpression m) return m.Member.Name;
    if (expr.Body is UnaryExpression u && u.Operand is MemberExpression um) return um.Member.Name;
    throw new InvalidOperationException("Invalid member expression");
  }
}
