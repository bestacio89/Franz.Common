#nullable enable
using Franz.Common.Business.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;


namespace Franz.Common.Http.Documentation.Extensions;

/// <summary>
/// Native OpenAPI schema extensions replacing the Swashbuckle
/// SwaggerGenOptions.ConvertEnumeration pattern.
/// Registers EnumerationClass types as OpenAPI-friendly schemas
/// via document transformers instead of SwaggerGen map overrides.
/// </summary>
public static class OpenApiSchemaExtensions
{
  /// <summary>
  /// Adds a document transformer that maps EnumerationClass types
  /// from all Contracts assemblies to their correct OpenAPI scalar types.
  /// </summary>
  public static OpenApiOptions ConvertEnumeration(this OpenApiOptions options)
  {
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
      var enumerationTypes = AppDomain.CurrentDomain
          .GetAssemblies()
          .Where(a => !a.IsDynamic)
          .Where(a => a.GetName().Name?.EndsWith("Contracts") == true)
          .SelectMany(a => a.DefinedTypes)
          .ToList();

      foreach (var type in enumerationTypes)
      {
        if (!type.IsEnumerationClass(out var genericType) || genericType is null)
          continue;

        var elementType = genericType.GenericTypeArguments.FirstOrDefault();
        if (elementType is null)
          continue;

        var schemaKey = type.Name;

        if (document.Components?.Schemas?.ContainsKey(schemaKey) == true)
          document.Components.Schemas[schemaKey] = BuildSchema(elementType);
      }

      return Task.CompletedTask;
    });

    return options;
  }

  private static OpenApiSchema BuildSchema(Type type)
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