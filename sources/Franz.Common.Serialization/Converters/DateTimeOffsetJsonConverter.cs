using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization.Converters;

public sealed class DateTimeOffsetJsonConverter
  : JsonConverter<DateTimeOffset?>
{
  private const string Format = "yyyy-MM-ddTHH:mm:ssZ";

  public override DateTimeOffset? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
  {
    if (reader.TokenType == JsonTokenType.Null)
      return null;

    if (reader.TokenType != JsonTokenType.String)
      throw new JsonException("Expected string for DateTimeOffset.");

    var raw = reader.GetString();

    if (string.IsNullOrWhiteSpace(raw))
      return null;

    if (DateTimeOffset.TryParse(
        raw,
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
        out var dto))
    {
      return dto;
    }

    throw new JsonException($"Invalid DateTimeOffset format: '{raw}'.");
  }

  public override void Write(
    Utf8JsonWriter writer,
    DateTimeOffset? value,
    JsonSerializerOptions options)
  {
    if (!value.HasValue)
    {
      writer.WriteNullValue();
      return;
    }

    writer.WriteStringValue(
      value.Value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
  }
}
