using Franz.Common.Reflection;
using Scrutor;
using System.Reflection;


namespace Franz.Common.DependencyInjection.Extensions;
public static class ITypeSourceSelectorExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IImplementationTypeSelector FromCompanyApplicationDependenciesWithPredicate(this ITypeSourceSelector typeSourceSelector, Func<Assembly, bool>? assemblyPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var companyPredicate = ReflectionHelper.GetAssemblyCompanyOrProductPredicate(assemblyPredicate);

    var result = typeSourceSelector.FromApplicationDependencies(assembly =>
    {
      var result = companyPredicate == null || companyPredicate(assembly);

      return result;
    });

    return result;
  }
}
