using System.Reflection;

namespace RabbitMQ.Client;

public static class ModelExtensions
{
  private const string FieldName = "_usesTransactions";

  public static bool HasTransaction(this IModel model)
  {
    var type = model.GetType();
    var field = type.GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;
    var value = field.GetValue(model);
    var result = Convert.ToBoolean(value);

    return result;
  }
}
