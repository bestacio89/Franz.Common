using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Franz.Common.Http.Headers;

public class HeaderComplexModelBinderProvider : IModelBinderProvider
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public IModelBinder? GetBinder(ModelBinderProviderContext context)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    IModelBinder? result = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    if (context.Metadata.IsComplexType)
    {
      var defaultModelMetadata = context.Metadata as DefaultModelMetadata;
      var headerAttribute = defaultModelMetadata?.Attributes.Attributes.FirstOrDefault(a => a.GetType() == typeof(FromHeaderAttribute));
      if (headerAttribute != null)
        result = new BinderTypeModelBinder(typeof(ComplexModelBinder));
    }

    return result;
  }
}
