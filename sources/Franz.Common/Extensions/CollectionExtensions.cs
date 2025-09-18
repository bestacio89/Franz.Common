namespace France.Common.Extensions;


public static class CollectionExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static bool IsNullOrEmpty<T>(this ICollection<T>? source)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    return source == null || !source.Any();
  }

  public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
  {
    var result = false;

    if (!source.Contains(item))
    {
      source.Add(item);
      result = true;
    }

    return result;
  }

  public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> enumerable)
  {
    if (enumerable != null)
    {
      foreach (var item in enumerable)
        source.Add(item);
    }
  }
}