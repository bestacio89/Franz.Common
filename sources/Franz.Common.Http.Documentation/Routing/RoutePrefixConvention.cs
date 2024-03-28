using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Franz.Common.Http.Documentation.Routing;
public class RoutePrefixConvention : IApplicationModelConvention
{
  private readonly AttributeRouteModel attributeRouteModel;

  public RoutePrefixConvention(IRouteTemplateProvider routeTemplateProvider)
  {
    attributeRouteModel = new AttributeRouteModel(routeTemplateProvider);
  }

  public void Apply(ApplicationModel applicationModel)
  {
    var selectors = applicationModel.Controllers.SelectMany(c => c.Selectors);

    foreach (var selector in selectors)
    {
      selector.AttributeRouteModel = selector.AttributeRouteModel != null
        ? AttributeRouteModel.CombineAttributeRouteModel(attributeRouteModel, selector.AttributeRouteModel)
        : attributeRouteModel;
    }
  }
}
