using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Franz.Common.Caching.Tests;

/// <summary>Reflection-based metadata for ordinary unit-test fixtures. Native AOT is tested separately with source-generated metadata.</summary>
internal static class TestJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
        => new(JsonSerializerDefaults.General) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
}
