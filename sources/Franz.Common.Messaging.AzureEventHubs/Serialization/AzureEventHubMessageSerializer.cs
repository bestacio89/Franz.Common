using Franz.Common.Messaging.Serialization;
using System.Text;

namespace Franz.Common.Messaging.AzureEventHubs.Serialization;

public sealed class AzureEventHubsMessageSerializer
{
  private readonly IMessageSerializer _serializer;

  public AzureEventHubsMessageSerializer(IMessageSerializer serializer)
  {
    _serializer = serializer;
  }

  public byte[] Serialize(string body)
    => Encoding.UTF8.GetBytes(body);

  public string Deserialize(byte[] data)
    => Encoding.UTF8.GetString(data);
}
