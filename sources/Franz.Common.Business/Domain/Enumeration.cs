using System.Reflection;

namespace Franz.Common.Business.Domain;

public abstract class Enumeration<TId> : IComparable<TId>
  where TId : notnull
{
  public string Name { get; private set; }

  public TId Id { get; private set; }

  protected Enumeration(TId id, string name)
  {
    Id = id;
    Name = name;
  }

  public override string ToString()
  {
    return Name.ToString();
  }

  public static IEnumerable<TEnumeration> GetAll<TEnumeration>() where TEnumeration : Enumeration<TId>
  {
    var fields = typeof(TEnumeration).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

    return fields.Select(f => f.GetValue(null)).Cast<TEnumeration>();
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public override bool Equals(object? obj)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (obj is not Enumeration<TId> otherValue)
      return false;

    var typeMatches = GetType().Equals(obj.GetType());
    var valueMatches = Id.Equals(otherValue.Id);

    var result = typeMatches && valueMatches;

    return result;
  }

  public override int GetHashCode()
  {
    return Id.GetHashCode();
  }

  public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
  {
    var result = Math.Abs(firstValue.Id - secondValue.Id);

    return result;
  }

  public static TEnumeration FromValue<TEnumeration>(TId value)
    where TEnumeration : Enumeration<TId>
  {
    var result = Parse<TEnumeration, TId>(value, "value", item => item.Id.Equals(value));

    return result;
  }

  public static TEnumeration FromDisplayName<TEnumeration>(string displayName)
    where TEnumeration : Enumeration<TId>
  {
    var result = Parse<TEnumeration, string>(displayName, "display name", item => item.Name == displayName);

    return result;
  }

  private static T Parse<T, K>(K value, string description, Func<T, bool> predicate)
    where T : Enumeration<TId>
  {
    var enumeration = GetAll<T>().FirstOrDefault(predicate);

    var result = enumeration ?? throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public int CompareTo(TId? other)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = other == null ? 1 : Name.CompareTo(other);

    return result;
  }
}

public abstract class Enumeration : Enumeration<int>
{
  protected Enumeration(int id, string name) : base(id, name)
  {
  }
}
