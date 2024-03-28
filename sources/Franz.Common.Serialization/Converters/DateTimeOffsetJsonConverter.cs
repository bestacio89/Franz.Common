using Newtonsoft.Json;
using System.Globalization;

namespace Franz.Common.Serialization.Converters;

public class DateTimeOffsetJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var result = objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);

        return result;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        DateTimeOffset? result = null;

        if (reader.Value is DateTimeOffset dateTimeOffset)
            result = dateTimeOffset;
        else if (reader.Value != null)
            throw new FormatException("Wrong Date Format");

        return result;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        var result = value as DateTimeOffset?;

        if (result.HasValue)
            writer.WriteValue(result.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
    }
}
