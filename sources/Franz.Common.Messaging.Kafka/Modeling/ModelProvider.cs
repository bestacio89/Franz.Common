#nullable enable
using Franz.Common.Messaging.Kafka.Connections;
using Franz.Common.Messaging.KafKa.Modeling;

namespace Franz.Common.Messaging.Kafka.Modeling;

public sealed class ModelProvider : IModelProvider, IDisposable
{
  private readonly IConnectionProvider connectionProvider;
  private KafkaModel? model;

  public ModelProvider(IConnectionProvider connectionProvider)
  {
    this.connectionProvider = connectionProvider;
  }

  public KafkaModel Current => GetCurrent();

  private KafkaModel GetCurrent()
  {
    if (model == null)
    {
      model = new KafkaModel(connectionProvider);
    }
    return model;
  }

  public void Dispose()
  {
    if (model != null)
      model.Dispose();
  }
}
