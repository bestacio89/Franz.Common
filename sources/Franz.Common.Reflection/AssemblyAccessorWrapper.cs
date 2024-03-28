using System.Reflection;

namespace Franz.Common.Reflection;

public class AssemblyAccessorWrapper : IAssemblyAccessor
{
    private readonly Assembly assembly;

    public AssemblyAccessorWrapper(Assembly assembly)
    {
        this.assembly = assembly;
    }

    public IAssembly GetEntryAssembly()
    {
        var result = new AssemblyWrapper(assembly);

        return result;
    }
}
