using Microsoft.AspNetCore.Mvc.ModelBinding;

using Newtonsoft.Json;

namespace Franz.Common.Http.Headers;

public class ComplexModelBinder : IModelBinder
{
  public Task BindModelAsync(ModelBindingContext bindingContext)
  {
    var headerKey = bindingContext.ModelMetadata.ParameterName;
    if (!string.IsNullOrEmpty(headerKey) &&
      bindingContext.HttpContext.Request.Headers.TryGetValue(headerKey, out var headerValue))
    {
      var modelType = bindingContext.ModelMetadata.ModelType;

      if (!string.IsNullOrEmpty(headerValue))
      {
#pragma warning disable CS8604 // Possible null reference argument.
        bindingContext.Model = JsonConvert.DeserializeObject(headerValue, modelType)!;
#pragma warning restore CS8604 // Possible null reference argument.
        bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
      }
    }

    return Task.CompletedTask;
  }
}
