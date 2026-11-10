using System.Text.Json.Serialization;

namespace Franz.Common.Messaging.Tests.Headers;

[JsonSerializable(typeof(HeaderContextAccessorTests.TestDto))]
internal partial class HeaderContextJsonContext : JsonSerializerContext
{
}
