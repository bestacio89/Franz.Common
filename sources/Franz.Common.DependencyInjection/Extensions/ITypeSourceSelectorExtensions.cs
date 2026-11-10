using Franz.Common.Reflection;
using Scrutor;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Franz.Common.DependencyInjection.Extensions;

public static class ITypeSourceSelectorExtensions
{
  /// <summary>
  /// Selects application assemblies by their company/product metadata.
  /// This operation discovers runtime assemblies and is not compatible with trimming.
  /// </summary>
  [RequiresUnreferencedCode("Application assembly discovery is runtime reflection. Native AOT applications must register services explicitly.")]
  public static IImplementationTypeSelector FromCompanyApplicationDependenciesWithPredicate(
      this ITypeSourceSelector typeSourceSelector,
      Func<Assembly, bool>? assemblyPredicate = null)
  {
    var companyPredicate = ReflectionHelper.GetAssemblyCompanyOrProductPredicate(assemblyPredicate);

    return typeSourceSelector.FromApplicationDependencies(assembly =>
        companyPredicate == null || companyPredicate(assembly));
  }
}