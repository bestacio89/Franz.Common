using Franz.Common.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Franz.Common.Http.MultiTenancy.Documentation;
public class AddRequiredHeaderParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.ActionConstraints?.OfType<HeaderRequiredAttribute>().Any() == true)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            foreach (var headerRequiredActionConstraint in context.ApiDescription.ActionDescriptor.ActionConstraints.OfType<HeaderRequiredAttribute>())
            {
                var parameter = new OpenApiParameter
                {
                    Name = headerRequiredActionConstraint.Name.ToLower(),
                    In = ParameterLocation.Header,
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                    Required = true,
                };
                operation.Parameters.Add(parameter);
            }
        }
    }
}
