#nullable enable
using Franz.Common.Headers;
using Franz.Common.Messaging.Messages;
using Franz.Common.MultiTenancy;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.MultiTenancy;

/// <summary>
/// Enriches messages with Tenant ID metadata.
/// Senior Architect Note: Refactored to IMessageBuilder async contract. 
/// Returns Task.CompletedTask as tenant resolution is an in-memory context operation.
/// </summary>
public class TenantMessageBuilder(ITenantContextAccessor? tenantContextAccessor = null)
    : IMessageBuilder
{
  private readonly ITenantContextAccessor? _tenantContextAccessor = tenantContextAccessor;

  /// <summary>
  /// Determines if the builder has an active tenant context to enrich the message.
  /// </summary>
  public bool CanBuild(Message message)
  {
    // Senior Note: .HasValue check remains synchronous as it targets local execution context.
    return _tenantContextAccessor?.GetCurrentTenantId().HasValue == true;
  }

  /// <summary>
  /// Asynchronously enriches the message headers with the current Tenant ID.
  /// </summary>
  public Task BuildAsync(Message message, CancellationToken ct = default)
  {
    var id = _tenantContextAccessor?.GetCurrentTenantId();

    if (id.HasValue)
    {
      // SENIOR FIX: Align with IDictionary<string, string[]> for JSON wire-format consistency.
      message.Headers[HeaderConstants.TenantId] = [id.Value.ToString()];
    }

    return Task.CompletedTask;
  }
}