using System.Text.Json;
using System.Text.Json.Serialization;

namespace Franz.Common.Serialization;

public static class FranzJson
{
  public static readonly JsonSerializerOptions Default = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,

    // SECURITY
    // No polymorphic deserialization
    // No type name handling
    // No custom converters unless explicit
  };
}
