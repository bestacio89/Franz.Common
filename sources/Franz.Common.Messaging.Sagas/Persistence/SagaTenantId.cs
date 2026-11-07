#nullable enable

namespace Franz.Common.Messaging.Sagas.Persistence;

/// <summary>
/// Validates tenant identifiers consistently before they participate in persistence keys.
/// A null value represents the default, non-tenant scope; supplied values must be non-blank.
/// </summary>
internal static class SagaTenantId
{
  public static string? Normalize(string? tenantId)
  {
    if (tenantId is not null && string.IsNullOrWhiteSpace(tenantId))
      throw new ArgumentException(
          "TenantId must be null or a non-empty, non-whitespace value.",
          nameof(tenantId));

    return tenantId;
  }
}
