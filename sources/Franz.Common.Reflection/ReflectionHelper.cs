using System.Reflection;

namespace Franz.Common.Reflection;

public static class ReflectionHelper
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static IEnumerable<Assembly> GetCurrentAppDomainAssemblies(Func<Assembly, bool>? predicate)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    var results = AppDomain.CurrentDomain
      .GetAssemblies()
      .Where(a => !a.IsDynamic);

    if (predicate != null)
      results = results.Where(predicate);

    return results;
  }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static Func<Assembly, bool>? GetAssemblyCompanyOrProductPredicate(Func<Assembly, bool>? optionalPredicate = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    bool result(Assembly assembly)
    {
      var result = (assembly.GetName()?.Name?.StartsWith(Company.Name, StringComparison.InvariantCultureIgnoreCase) == true ||
        assembly.GetName()?.Name?.StartsWith(ProductGeneration.Name, StringComparison.InvariantCultureIgnoreCase) == true) &&
        (optionalPredicate == null || optionalPredicate(assembly));

      return result;
    }

    return result;
  }
}
