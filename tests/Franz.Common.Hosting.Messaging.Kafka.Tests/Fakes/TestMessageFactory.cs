using Franz.Common.Hosting.Messaging.Kafka.Tests.Events;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Franz.Common.Serialization;

namespace Franz.Common.Hosting.Messaging.Kafka.Tests.Fakes;

internal static class TestMessageFactory
{
  public static Message FromEvent(TestEvent evt, IMessageSerializer serializer)
  {
    return new Message
    {
      MessageType = nameof(TestEvent),
      Body = serializer.Serialize(evt)
    };
  }
}
