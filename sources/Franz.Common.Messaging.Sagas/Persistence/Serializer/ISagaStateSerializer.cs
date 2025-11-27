using Franz.Common.DependencyInjection;
using System;

namespace Franz.Common.Messaging.Sagas.Persistence.Serializer;

/// <summary>
/// Serializes and deserializes saga state objects for persistence providers.
/// </summary>
public interface ISagaStateSerializer : IScopedDependency
{
  string Serialize(object state);
  object Deserialize(string payload, Type targetType);
}
