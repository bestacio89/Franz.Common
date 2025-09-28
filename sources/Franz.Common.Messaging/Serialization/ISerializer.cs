namespace Franz.Common.Messaging.Serialization;

public interface IMessageSerializer
{
  string Serialize<T>(T obj);
  T? Deserialize<T>(string data);
  object? Deserialize(string data, Type type);
}
