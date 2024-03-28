using Franz.Common.Http.Documentation.Configuration;
using Franz.Common.Http.Documentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDocumentation(this IServiceCollection services)
  {
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
      var xmlFilename = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
      options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
      options.ConvertEnumeration();
    });
    services.ConfigureOptions<ConfigureSwaggerOptions>();
    services.AddControllersWithViews(options => options.UseGeneralRoutePrefix("api/v{version:apiVersion}"));
    services.AddApiVersioning(opt =>
    {
      opt.DefaultApiVersion = new ApiVersion(1, 0);
      opt.AssumeDefaultVersionWhenUnspecified = true;
      opt.ReportApiVersions = true;
      opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                      new HeaderApiVersionReader("x-api-version"),
                                                      new MediaTypeApiVersionReader("x-api-version"));
    });
    services.AddVersionedApiExplorer(setup =>
    {
      setup.GroupNameFormat = "'v'VVV";
      setup.SubstituteApiVersionInUrl = true;
    });

    return services;
  }
}
