using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Franz.Common.Business.Domain;

namespace Franz.Common.Serialization.Converters;

public sealed class EnumerationJsonConverter : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    return IsEnumeration(typeToConvert);
  }

  public override JsonConverter CreateConverter(
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    var converterType = typeof(EnumerationJsonConverter)
      .MakeGenericType(typeToConvert);

    return (JsonConverter)Activator.CreateInstance(converterType)!;
  }

  private static bool IsEnumeration(Type type)
  {
    while (type != null)
    {
      if (type.IsGenericType &&
          type.GetGenericTypeDefinition() == typeof(Enumeration<>))
        return true;

      type = type.BaseType!;
    }

    return false;
  }
}
