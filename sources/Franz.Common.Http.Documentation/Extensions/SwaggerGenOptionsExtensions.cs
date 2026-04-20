#nullable enable
using Franz.Common.Business.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Franz.Common.Http.Documentation.Extensions;

public static class SwaggerGenOptionsExtensions
{
  /// <summary>
  /// Converts all EnumerationClass types from Contracts assemblies to OpenAPI-friendly types.
  /// </summary>
  public static SwaggerGenOptions ConvertEnumeration(this SwaggerGenOptions options)
  {
    var types = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic)
        .Where(a => a.GetName().Name?.EndsWith("Contracts") == true)
        .SelectMany(a => a.DefinedTypes)
        .ToList();

    foreach (var type in types)
    {
      if (type.IsEnumerationClass(out var genericType) && genericType != null)
      {
        var elementType = genericType.GenericTypeArguments.FirstOrDefault();
        if (elementType != null)
        {
          var capturedType = elementType; // avoid closure capture of loop variable
          options.MapType(type, () => GenerateOpenApiSchema(capturedType));
        }
      }
    }

    return options;
  }

  private static OpenApiSchema GenerateOpenApiSchema(Type type)
  {
    if (type == typeof(short) || type == typeof(int))
      return new OpenApiSchema
      {
        Type = JsonSchemaType.Integer,
        Format = "int32"
      };

    if (type == typeof(long))
      return new OpenApiSchema
      {
        Type = JsonSchemaType.Integer,
        Format = "int64"
      };

    return new OpenApiSchema
    {
      Type = JsonSchemaType.String
    };
  }
}