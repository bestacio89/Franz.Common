# Franz.Common.Serialization

> System.Text.Json serialization contracts with explicit converter composition and UTF-8 byte-array support.

## Installation

```bash
dotnet add package Franz.Common.Serialization
```

## Register serializer services

```csharp
using Franz.Common.Serialization.Extensions;

builder.Services.AddSerializers();
```

The extension registers:

- `IJsonSerializer` → `SystemTextJsonSerializer` as a singleton.
- `IByteArraySerializer` → `ByteArraySerializer` as a singleton.

Both registrations use the Franz no-duplicate registration strategy. The JSON serializer receives the registered `IEnumerable<JsonConverter>`; add converters explicitly when the application requires them.

## JSON contract

`IJsonSerializer` exposes:

```csharp
string? Serialize(object? content);
T? Deserialize<T>(string? content);
object? Deserialize(string? content, Type targetType);
```

The current implementation uses `System.Text.Json`, not Newtonsoft.Json. Its options use camel-case property naming, case-insensitive property matching, omit null-valued properties when writing, and produce compact JSON. Null input serializes to null; blank input deserializes to the default value/null.

```csharp
using Franz.Common.Serialization;

public sealed class EventPublisher(IJsonSerializer serializer)
{
    public string? Serialize(OrderCreated message) =>
        serializer.Serialize(message);

    public OrderCreated? Deserialize(string payload) =>
        serializer.Deserialize<OrderCreated>(payload);
}
```

## Default options

`FranzJson.Default` exposes a static `JsonSerializerOptions` instance with the same camel-case, case-insensitive, null-omitting, compact defaults. It intentionally does not enable polymorphic type-name deserialization or add custom converters by default.

The registered `SystemTextJsonSerializer` creates its own options instance and adds the converters supplied through DI; it does not use `FranzJson.Default` directly.

## Custom converters

The assembly contains converter types such as `DateTimeJsonConverter`, `DateTimeOffsetJsonConverter`, and `EnumerationJsonConverter`. Their presence does not mean they are automatically registered. Register the required `JsonConverter` instances with DI before calling `AddSerializers()`, or configure a serializer instance explicitly where appropriate.

## Byte-array contract

`ByteArraySerializer` serializes an object through `IJsonSerializer`, then encodes the JSON as UTF-8 bytes. Deserialization decodes UTF-8 and delegates to the JSON serializer. A null object produces an empty byte array; null byte input produces the default result.

This is JSON encoded as UTF-8, not a binary object serializer or a compression format. Do not use it when a protocol requires a distinct binary schema.

## Security and compatibility

Do not add polymorphic type activation for untrusted payloads without a strict allow-list and a reviewed threat model. Treat converter behavior, naming policy, null handling, and date/time representation as wire-contract decisions; cover changes with compatibility tests.

## License

MIT. See [LICENSE](../../LICENSE).
