using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Franz.Common.Serialization;

public abstract class JsonCreationConverter<T> : JsonConverter
{
    public override bool CanWrite => false;

    public override bool CanConvert(Type objectType)
    {
        return typeof(T).IsAssignableFrom(objectType);
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        var jObject = JObject.Load(reader);

        var target = Create(objectType, jObject);

#pragma warning disable CS8604 // Possible null reference argument.
        serializer.Populate(jObject.CreateReader(), target);
#pragma warning restore CS8604 // Possible null reference argument.

        return target;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        throw new NotImplementedException();
    }

    protected abstract T Create(Type objectType, JObject jObject);
}
