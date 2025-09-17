using Franz.Common.Http.Documentation.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Franz.Common.Http.Documentation.Extensions;

public static class MvcOptionsExtensions
{
  public static void UseGeneralRoutePrefix(this MvcOptions mvcOptions, IRouteTemplateProvider routeTemplateProvider)
  {
    mvcOptions.Conventions.Add(new RoutePrefixConvention(routeTemplateProvider));
  }

  public static void UseGeneralRoutePrefix(this MvcOptions mvcOptions, string prefix)
  {
    mvcOptions.UseGeneralRoutePrefix(new RouteAttribute(prefix));
  }
}
