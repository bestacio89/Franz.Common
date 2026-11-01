using System.Diagnostics.CodeAnalysis;

namespace Franz.Common.Reflection.Extensions;

public static class TypeExtensions
{
    [RequiresUnreferencedCode("Runtime type inspection requires interface metadata to be preserved. Use compile-time contracts for trimmed applications.")]
    public static bool Implements<TInterface>(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var interfaceType = typeof(TInterface);
        return type.GetInterfaces().Contains(interfaceType);
    }

    [RequiresUnreferencedCode("Runtime type inspection requires interface metadata to be preserved. Use compile-time contracts for trimmed applications.")]
    public static bool DirectlyImplements<TInterface>(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var searchInterfaceType = typeof(TInterface);
        var minimalInterfaces = type.GetInterfaces();
        var baseType = type.BaseType;

        if (baseType is not null)
            minimalInterfaces = minimalInterfaces.Except(baseType.GetInterfaces()).ToArray();

        return minimalInterfaces.Any(searchInterfaceType.IsAssignableFrom);
    }
}
