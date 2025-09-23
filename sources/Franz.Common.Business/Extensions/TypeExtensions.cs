#nullable enable
using Franz.Common.Business.Domain;

namespace Franz.Common.Business.Extensions;

public static class TypeExtensions
{
  public static bool IsEnumerationClass(this Type type)
  {
    var result = IsEnumerationClass(type, out _);
    return result;
  }

  public static bool IsEnumerationClass(this Type type, out Type? genericType)
  {
    genericType = FirstGenericType(type);

    var result = genericType is not null &&
                 genericType.GetGenericTypeDefinition() == typeof(Enumeration<>);

    return result;
  }

  private static Type? FirstGenericType(this Type type)
  {
    var result = type.IsGenericType
      ? type
      : type.BaseType is not null ? FirstGenericType(type.BaseType) : null;

    return result;
  }
}
