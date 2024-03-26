using Franz.Common.Business.Domain;

namespace System;
public static class TypeExtensions
{
  public static bool IsEnumerationClass(this Type type)
  {
    var result = IsEnumerationClass(type, out var _);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static bool IsEnumerationClass(this Type type, out Type? genericType)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    genericType = FirstGenericType(type);

    var result = genericType is not null && genericType.GetGenericTypeDefinition() == typeof(Enumeration<>);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private static Type? FirstGenericType(this Type type)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = type.IsGenericType
      ? type
      : type.BaseType is not null ? FirstGenericType(type.BaseType) : null;

    return result;
  }

}
