using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Franz.Common.Business.Domain;

namespace Franz.Common.Serialization.Converters;

/// <summary>
/// Native AOT-compatible converter for a closed Franz enumeration type.
/// Register an instance explicitly in the application's source-generated JSON options.
/// </summary>
public sealed class EnumerationValueJsonConverter<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnumeration,
    TId> : JsonConverter<TEnumeration>
    where TEnumeration : Enumeration<TId>
    where TId : notnull, IComparable<TId>
{
  public override TEnumeration Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options)
  {
    var idTypeInfo = GetIdTypeInfo(options);
    var id = JsonSerializer.Deserialize(ref reader, idTypeInfo);

    if (id is null)
      throw new JsonException($"Enumeration identifier for '{typeof(TEnumeration)}' cannot be null.");

    try
    {
      return Enumeration<TId>.FromValue<TEnumeration>(id);
    }
    catch (InvalidOperationException exception)
    {
      throw new JsonException(
          $"The identifier is not a valid value for enumeration '{typeof(TEnumeration)}'.",
          exception);
    }
  }

  public override void Write(
      Utf8JsonWriter writer,
      TEnumeration value,
      JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(value);
    JsonSerializer.Serialize(writer, value.Id, GetIdTypeInfo(options));
  }

  private static JsonTypeInfo<TId> GetIdTypeInfo(JsonSerializerOptions options)
      => options.GetTypeInfo(typeof(TId)) as JsonTypeInfo<TId>
          ?? throw new InvalidOperationException(
              $"The configured JSON source-generation context does not contain metadata for '{typeof(TId)}'.");
}