namespace Franz.Common.Extensions;

public static class EnumerableExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static void ForEach<T>(this IEnumerable<T>? list, Action<T> action)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  {
    if (list != null)
    {
      foreach (var t in list)
        action(t);
    }
  }
}
