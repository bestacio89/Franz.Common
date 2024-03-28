using Confluent.Kafka;

namespace Confluent.Kafka.Extensions
{
  public static class ModelExtensions
  {
    public static bool HasTransaction(this IProducer<byte[], byte[]> model)
    {
      return model.HasTransaction();
    }
  }
}