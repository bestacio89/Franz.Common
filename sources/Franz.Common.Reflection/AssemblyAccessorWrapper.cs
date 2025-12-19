using System.Reflection;

namespace Franz.Common.Reflection;

public sealed class AssemblyAccessorWrapper : IAssemblyAccessor
{
  public IAssembly GetEntryAssembly()
  {
    var assembly =
        Assembly.GetEntryAssembly()
        ?? Assembly.GetExecutingAssembly();

    return new AssemblyWrapper(assembly);
  }
}
