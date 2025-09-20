using System.Reflection;

namespace Franz.Common.Business.Domain;

public abstract class Enumeration<TId> : IComparable<Enumeration<TId>>
    where TId : notnull, IComparable<TId>
{
  public string Name { get; }
  public TId Id { get; }

  protected Enumeration(TId id, string name)
  {
    Id = id;
    Name = name;
  }

  public override string ToString() => Name;

  private static readonly Dictionary<Type, object> _cache = new();

  public static IReadOnlyCollection<TEnumeration> GetAll<TEnumeration>()
      where TEnumeration : Enumeration<TId>
  {
    var type = typeof(TEnumeration);

    if (_cache.TryGetValue(type, out var cached))
      return (IReadOnlyCollection<TEnumeration>)cached;

    var fields = type
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(f => type.IsAssignableFrom(f.FieldType))
        .Select(f => f.GetValue(null))
        .Cast<TEnumeration>()
        .ToArray();

    _cache[type] = fields;

    return fields;
  }

  public override bool Equals(object? obj)
  {
    if (obj is not Enumeration<TId> other) return false;

    return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
  }

  public override int GetHashCode() => Id.GetHashCode();

  public static TEnumeration FromValue<TEnumeration>(TId value)
      where TEnumeration : Enumeration<TId>
  {
    return Parse<TEnumeration, TId>(value, "value", item => item.Id.Equals(value));
  }

  public static TEnumeration FromDisplayName<TEnumeration>(string displayName)
      where TEnumeration : Enumeration<TId>
  {
    return Parse<TEnumeration, string>(displayName, "display name", item => item.Name == displayName);
  }

  private static T Parse<T, K>(K value, string description, Func<T, bool> predicate)
      where T : Enumeration<TId>
  {
    var enumeration = GetAll<T>().FirstOrDefault(predicate);

    return enumeration ?? throw new InvalidOperationException(
        $"'{value}' is not a valid {description} in {typeof(T)}");
  }

  public int CompareTo(Enumeration<TId>? other)
  {
    if (other is null) return 1;
    return Id.CompareTo(other.Id);
  }

  // Numeric-only utility
  public static int AbsoluteDifference(Enumeration<int> first, Enumeration<int> second)
      => Math.Abs(first.Id - second.Id);
}

public abstract class Enumeration : Enumeration<int>
{
  protected Enumeration(int id, string name) : base(id, name) { }
}
