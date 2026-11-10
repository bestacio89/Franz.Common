using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Franz.Common.Business.Domain;

namespace Franz.Common.Serialization.Converters;

/// <summary>
/// Reflection-based convenience factory for domain enumeration converters.
/// Native AOT applications should register closed
/// <see cref="EnumerationValueJsonConverter{TEnumeration, TId}"/> instances directly.
/// </summary>
public sealed class EnumerationJsonConverter : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
      => FindEnumerationBase(typeToConvert) is not null;

  [RequiresUnreferencedCode("The factory discovers enumeration base types and closes a converter at runtime. Native AOT applications must register EnumerationValueJsonConverter<TEnumeration, TId> directly.")]
  [RequiresDynamicCode("The factory closes a generic converter at runtime. Native AOT applications must register EnumerationValueJsonConverter<TEnumeration, TId> directly.")]
  public override JsonConverter CreateConverter(
      Type typeToConvert,
      JsonSerializerOptions options)
  {
    var enumerationBase = FindEnumerationBase(typeToConvert)
        ?? throw new InvalidOperationException(
            $"'{typeToConvert}' is not a Franz enumeration type.");

    var idType = enumerationBase.GetGenericArguments()[0];
    var converterType = typeof(EnumerationValueJsonConverter<,>)
        .MakeGenericType(typeToConvert, idType);

    return (JsonConverter)(Activator.CreateInstance(converterType)
        ?? throw new InvalidOperationException(
            $"Could not create an enumeration converter for '{typeToConvert}'."));
  }

  private static Type? FindEnumerationBase(Type type)
  {
    for (var current = type; current is not null; current = current.BaseType)
    {
      if (current.IsGenericType &&
          current.GetGenericTypeDefinition() == typeof(Enumeration<>))
        return current;
    }

    return null;
  }
}