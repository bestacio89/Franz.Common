namespace Franz.Common.Reflection.Extensions;

public static class TypeExtensions
{
    public static bool Implements<TInterface>(this Type type)
    {
        var interfaceType = typeof(TInterface);

        var result = type.GetInterfaces().Contains(interfaceType);

        return result;
    }

    public static bool DirectlyImplements<TInterface>(this Type type)
    {
        var searchInterfaceType = typeof(TInterface);

        var minimalInterfaces = type.GetInterfaces();
        if (type.BaseType != null)
            minimalInterfaces = minimalInterfaces.AsQueryable().Except(type.BaseType.GetInterfaces()).ToArray();

        var result = minimalInterfaces.Any(i => searchInterfaceType.IsAssignableFrom(i));

        return result;
    }
}
