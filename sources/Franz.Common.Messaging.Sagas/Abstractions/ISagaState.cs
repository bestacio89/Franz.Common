#nullable enable

namespace Franz.Common.Messaging.Sagas.Abstractions;

/// <summary>
/// Saga state is PURE DATA. No identity responsibility.
/// </summary>
public interface ISagaState
{
  string? ConcurrencyToken { get; set; }
}