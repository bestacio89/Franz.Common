using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Franz.Common.Mapping.Abstractions;

namespace Franz.Common.Mapping.Extensions;

public static class QueryableExtensions
{
    [RequiresUnreferencedCode("Runtime projection discovery uses reflection. Use the strongly typed ProjectTo overload for trimming.")]
    [RequiresDynamicCode("Runtime projection discovery constructs closed generic types. Use the strongly typed ProjectTo overload for Native AOT.")]
    public static IQueryable<TDestination> ProjectTo<TDestination>(this IQueryable source, IServiceProvider serviceProvider)
    {
        var sourceType = source.ElementType;
        var projectionType = typeof(IProjection<,>).MakeGenericType(sourceType, typeof(TDestination));
        var projection = serviceProvider.GetRequiredService(projectionType);
        var projectMethod = projectionType.GetMethod(nameof(IProjection<object, object>.Project));
        return (IQueryable<TDestination>)projectMethod!.Invoke(projection, new[] { source })!;
    }

    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        this IQueryable<TSource> source, 
        IProjection<TSource, TDestination> projection)
    {
        return projection.Project(source);
    }
}
