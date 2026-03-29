using System.Linq;

namespace Franz.Common.Mapping.Abstractions;

public interface IProjection<in TSource, out TDestination>
{
    IQueryable<TDestination> Project(IQueryable<TSource> queryable);
}
