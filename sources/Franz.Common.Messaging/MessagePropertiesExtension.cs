using Franz.Common.Messaging.Messages;

namespace Franz.Common.Messaging;

public static class MessagePropertiesExtensions
{
  public static bool TryGetProperty<T>(this Message message, string key, out T value)
  {
    value = default!;
    if (message.Properties.TryGetValue(key, out var raw))
    {
      if (raw is T casted)
      {
        value = casted;
        return true;
      }

      // try convert if stored as string but requested T is string
      if (typeof(T) == typeof(string) && raw is not null)
      {
        value = (T)(object)raw.ToString()!;
        return true;
      }
    }
    return false;
  }

  public static void SetProperty<T>(this Message message, string key, T value)
  {
    message.Properties[key] = value!;
  }
}
