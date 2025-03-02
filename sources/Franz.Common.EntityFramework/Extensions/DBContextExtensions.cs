using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

public static class DbContextExtensions
{
  public static IQueryable<T> IncludeAllRelationships<T>(this IQueryable<T> query, DbContext context)
      where T : class
  {
    var entityType = context.Model.FindEntityType(typeof(T));

    if (entityType == null)
      return query;

    foreach (var navigation in entityType.GetNavigations())
    {
      query = query.Include(navigation.Name);
    }

    return query;
  }
}
