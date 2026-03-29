#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>
/// Enriches messages with Domain ID metadata from the current context.
/// Senior Architect Note: Updated to IMessageBuilder async contract. Using Task.CompletedTask 
/// as domain resolution from context accessors is typically an in-memory operation.
/// </summary>
public class DomainMessageBuilder(IDomainContextAccessor? domainContextAccessor = null)
    : IMessageBuilder
{
  private readonly IDomainContextAccessor? _domainContextAccessor = domainContextAccessor;

  /// <summary>
  /// Determines if the builder can proceed based on the availability of a Domain ID.
  /// </summary>
  public bool CanBuild(Message message)
  {
    // Senior Note: .HasValue check remains synchronous as it targets local execution context.
    return _domainContextAccessor?.GetCurrentDomainId().HasValue == true;
  }

  /// <summary>
  /// Asynchronously enriches the message headers with the current Domain ID.
  /// </summary>
  public Task BuildAsync(Message message, CancellationToken ct = default)
  {
    var id = _domainContextAccessor?.GetCurrentDomainId();

    if (id.HasValue)
    {
      // Consistent use of string array for header values to support multi-value transport standards.
      message.Headers[HeaderConstants.DomainId] = new[] { id.Value.ToString() };
    }

    return Task.CompletedTask;
  }
}