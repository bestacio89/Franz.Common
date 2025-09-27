namespace Franz.Common.Identity
{
  public class FranzIdentityContext
  {
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string[]? Roles { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? DomainId { get; init; }
  }
}
