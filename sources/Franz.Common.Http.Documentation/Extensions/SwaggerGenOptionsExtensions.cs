using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Franz.Common.Http.Documentation.Extensions;
public static class SwaggerGenOptionsExtensions
{
  public static SwaggerGenOptions ConvertEnumeration(this SwaggerGenOptions options)
  {
    var types = AppDomain.CurrentDomain.GetAssemblies()
      .Where(assembly => !assembly.IsDynamic)
      .Where(assembly => assembly.GetName().Name?.EndsWith("Contrats") == true)
      .SelectMany(assembly => assembly.DefinedTypes)
      .ToList();

    foreach (var type in types)
    {
      if (type.IsEnumerationClass(out var generictype))
        options.MapType(type, () => Generate(generictype!.GenericTypeArguments.First()));
    }

    return options;
  }

  private static OpenApiSchema Generate(Type type)
  {
    var result = type.Equals(typeof(short)) | type.Equals(typeof(int)) | type.Equals(typeof(long))
      ? new OpenApiSchema { Type = "integer", Format = type.Name.ToLower() }
      : new OpenApiSchema { Type = type.Name };

    return result;
  }
}
