using Franz.Common.Http.Headers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Immutable;

namespace Franz.Common.Http.MultiTenancy.Documentation;

public sealed class AddRequiredHeaderParameter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    // Retrieve attribute from endpoint metadata
    var headerAttributes = context.ApiDescription
        .ActionDescriptor
        .EndpointMetadata
        .OfType<HeaderRequiredAttribute>()
        .ToList();

    if (headerAttributes.Count == 0)
      return;

    // Ensure list exists
    if(operation.Parameters == null)
            operation.Parameters = new List<IOpenApiParameter>();

    foreach (var attr in headerAttributes)
    {
      operation.Parameters.Add(new OpenApiParameter
      {
        Name = attr.Name,
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema
        {
          // Minimal OpenAPI Schema only has object? Type,
          // So we MUST set it to a string literal.
          Type = JsonSchemaType.String// ✔ This is legal for Microsoft.OpenApi.OpenApiSchema
        }
      });
    }
  }
}
