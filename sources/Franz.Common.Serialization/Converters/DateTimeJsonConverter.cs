using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization.Converters;

public sealed class DateTimeJsonConverter : JsonConverter<DateTime>
{
  private const string Format = "yyyy-MM-ddTHH:mm:ssZ";

  public override DateTime Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.String)
      throw new JsonException("Expected string token for DateTime.");

    var value = reader.GetString();

    if (string.IsNullOrWhiteSpace(value))
      throw new JsonException("DateTime value cannot be null or empty.");

    if (!DateTime.TryParseExact(
          value,
          Format,
          CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
          out var result))
    {
      throw new JsonException($"Invalid DateTime format. Expected '{Format}'.");
    }

    return result;
  }

  public override void Write(
    Utf8JsonWriter writer,
    DateTime value,
    JsonSerializerOptions options)
  {
    writer.WriteStringValue(
      value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
  }
}
