using Microsoft.AspNetCore.Routing;

namespace Franz.Common.Http.Routing;

public class TranslateControllerParameterTransformer : IOutboundParameterTransformer
{
  private readonly string controllerTranslation;

  public TranslateControllerParameterTransformer(string controllerTranslation)
  {
    this.controllerTranslation = controllerTranslation;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public string? TransformOutbound(object? value)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var result = value as string;
    if (result != null)
      result = result.Replace(controllerTranslation, string.Empty, StringComparison.InvariantCulture).ToLowerInvariant();

    return result;
  }
}
