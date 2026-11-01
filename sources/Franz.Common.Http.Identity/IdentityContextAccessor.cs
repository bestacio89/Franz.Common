using Franz.Common.Identity;
using Franz.Common.MultiTenancy;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Franz.Common.Http.Identity;

/// <summary>
/// Reads user identity only from an authenticated principal and tenant/domain identity
/// only from the validated tenant execution context.
/// </summary>
public sealed class IdentityContextAccessor : IIdentityContextAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITenantExecutionContextAccessor? tenantExecutionContextAccessor;

    public IdentityContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        ITenantExecutionContextAccessor? tenantExecutionContextAccessor = null)
    {
        this.httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.tenantExecutionContextAccessor = tenantExecutionContextAccessor;
    }

    public string? GetCurrentEmail() => GetAuthenticatedClaim(ClaimTypes.Email);

    public Guid? GetCurrentId()
    {
        var userId = GetAuthenticatedClaim(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    public string? GetCurrentFullName() => GetAuthenticatedClaim(ClaimTypes.Name);

    public string[]? GetCurrentRoles()
    {
        var principal = GetAuthenticatedPrincipal();
        if (principal is null)
            return null;

        var roles = principal.FindAll(ClaimTypes.Role)
            .SelectMany(claim => claim.Value.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return roles.Length == 0 ? null : roles;
    }

    public Guid? GetCurrentTenantId() => tenantExecutionContextAccessor?.Current?.TenantId;

    public Guid? GetCurrentDomainId() => tenantExecutionContextAccessor?.Current?.DomainId;

    public FranzIdentityContext GetCurrentIdentity()
        => new()
        {
            UserId = GetCurrentId(),
            Email = GetCurrentEmail(),
            FullName = GetCurrentFullName(),
            Roles = GetCurrentRoles(),
            TenantId = GetCurrentTenantId(),
            DomainId = GetCurrentDomainId()
        };

    private string? GetAuthenticatedClaim(string claimType)
        => GetAuthenticatedPrincipal()?.FindFirst(claimType)?.Value;

    private ClaimsPrincipal? GetAuthenticatedPrincipal()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        return principal?.Identity?.IsAuthenticated == true ? principal : null;
    }
}
