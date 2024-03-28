using Franz.Common.Http.Properties;

namespace Franz.Common.Http.Routing;

public class FrenchControllerParameterTransformer : TranslateControllerParameterTransformer
{
  public FrenchControllerParameterTransformer()
    : base(Resources.FrenchNameController)
  {
  }
}
