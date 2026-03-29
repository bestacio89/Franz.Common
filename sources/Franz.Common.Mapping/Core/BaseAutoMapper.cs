using System;
using System.Reflection;
using Franz.Common.Mapping.Abstractions;
using Franz.Common.Reflection.Extensions;

namespace Franz.Common.Mapping.Core;

public class BaseAutoMapper<TSource, TDestination> : IMapper<TSource, TDestination>
    where TDestination : new()
{
    public virtual TDestination Map(TSource source)
    {
        if (source == null) return default!;

        var destination = new TDestination();
        var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var destProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var destProp in destProperties)
        {
            if (!destProp.CanWrite) continue;

            foreach (var srcProp in sourceProperties)
            {
                if (srcProp.Name.Equals(destProp.Name, StringComparison.OrdinalIgnoreCase) && 
                    destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType) && 
                    srcProp.CanRead)
                {
                    try
                    {
                        var value = srcProp.GetValue(source);
                        destProp.SetValue(destination, value);
                    }
                    catch
                    {
                        // Ignore property-level mapping failures in fallback scenarios
                    }
                    break;
                }
            }
        }

        return destination;
    }
}
