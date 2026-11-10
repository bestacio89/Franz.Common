namespace Franz.Common.Messaging.Serialization;

public interface IMessageSerializer
{
  string Serialize<T>(T obj);

  /// <summary>
  /// Serializes a value using its concrete runtime type.
  /// Implementations should use registered metadata for Native AOT scenarios.
  /// </summary>
  string Serialize(object obj, Type type) => Serialize(obj);

  T? Deserialize<T>(string data);
  object? Deserialize(string data, Type type);
}
