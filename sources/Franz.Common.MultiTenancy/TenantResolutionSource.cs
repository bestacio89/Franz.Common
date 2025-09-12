namespace Franz.Common.MultiTenancy
{
  public enum TenantResolutionSource
  {
    Unknown,
    Header,         // e.g. HTTP or Messaging headers
    Host,           // e.g. domain hostnames
    QueryString,    // e.g. ?tenantId=123
    JwtClaim,       // e.g. tenant_id claim in JWT
    Cookie,         // e.g. tenant info in cookies
    Property,       // ✅ NEW: application/message properties
    Default         // fallback
  }
}
