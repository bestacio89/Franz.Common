using Franz.Common.Business.Domain;
using Newtonsoft.Json;
using System.Reflection;

namespace Franz.Common.Serialization.Converters;
public class EnumerationJsonConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    var genericType = FirstGenericType(objectType);

    var result = genericType is not null && genericType == typeof(Enumeration<>);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private Type? FirstGenericType(Type objectType)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = objectType.IsGenericType
      ? objectType.GetGenericTypeDefinition()
      : objectType.BaseType is not null ? FirstGenericType(objectType.BaseType) : null;

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    object? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    if (reader.Value is not null)
      result = GetValue(objectType, reader.Value!);

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  private object? GetValue(Type enumerationType, object? id)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var values = enumerationType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
    var property = enumerationType.GetProperty(nameof(Enumeration.Id))!;

    var result = values
      .Select(value => value.GetValue(null))
      .Single(enumeration => property.GetValue(enumeration)!.ToString() == id?.ToString());

    return result;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (value is not null)
    {
      var property = value.GetType().GetProperty(nameof(Enumeration.Id))!;
      var enumeration = property.GetValue(value);

      writer.WriteValue(enumeration);
    }
  }
}
