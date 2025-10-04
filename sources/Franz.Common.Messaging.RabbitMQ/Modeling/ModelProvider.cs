using Franz.Common.Messaging.RabbitMQ.Connections;
using RabbitMQ.Client;

namespace Franz.Common.Messaging.RabbitMQ.Modeling;

public sealed class ModelProvider : IModelProvider, IDisposable
{
  private readonly IConnectionProvider connectionProvider;
  private IModel? model;

  public ModelProvider(IConnectionProvider connectionProvider)
  {
    this.connectionProvider = connectionProvider;
  }

  public IModel Current => GetCurrent();

  private IModel GetCurrent()
  {
    if (model == null)
    {
      model = connectionProvider.Current.CreateModel();
      model.BasicQos(0, 1, false);
    }

    return model;
  }

  public void Dispose()
  {
    if (model != null)
      model.Dispose();
  }
}
