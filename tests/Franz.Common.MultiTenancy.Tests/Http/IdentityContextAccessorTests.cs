using System.Security.Claims;
using Franz.Common.Http.Identity;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.Identity;
using Microsoft.AspNetCore.Http;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class IdentityContextAccessorTests
{
    [Fact]
    public void UnauthenticatedRequestHeadersCannotEstablishIdentityOrTenant()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();
        context.Request.Headers["X-User-Email"] = "attacker@example.test";
        context.Request.Headers["X-User-FullName"] = "Attacker";
        context.Request.Headers["X-User-Roles"] = "Admin";
        context.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();
        context.Request.Headers["X-Domain-Id"] = Guid.NewGuid().ToString();

        var accessor = new IdentityContextAccessor(new HttpContextAccessor { HttpContext = context });

        Assert.Null(accessor.GetCurrentId());
        Assert.Null(accessor.GetCurrentEmail());
        Assert.Null(accessor.GetCurrentFullName());
        Assert.Null(accessor.GetCurrentRoles());
        Assert.Null(accessor.GetCurrentTenantId());
        Assert.Null(accessor.GetCurrentDomainId());
    }

    [Fact]
    public void AuthenticatedPrincipalProvidesUserIdentityClaims()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, "user@example.test"),
                new Claim(ClaimTypes.Name, "Authenticated User"),
                new Claim(ClaimTypes.Role, "Reader"),
                new Claim(ClaimTypes.Role, "Writer"),
                new Claim("role", "Auditor")
            ],
            authenticationType: "test"))
        };
        context.Request.Headers["X-User-Id"] = Guid.NewGuid().ToString();
        context.Request.Headers["X-User-Email"] = "spoofed@example.test";
        context.Request.Headers["X-User-Roles"] = "Administrator";

        var accessor = new IdentityContextAccessor(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(userId, accessor.GetCurrentId());
        Assert.Equal("user@example.test", accessor.GetCurrentEmail());
        Assert.Equal("Authenticated User", accessor.GetCurrentFullName());
        Assert.Equal(new[] { "Reader", "Writer", "Auditor" }, accessor.GetCurrentRoles());
    }

    [Fact]
    public void TenantAndDomainIdentityComeFromValidatedExecutionContext()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var domainId = Guid.NewGuid();
        var executionContext = new TenantExecutionContext(
            tenant,
            domainId,
            TenantResolutionSource.Host,
            DateTimeOffset.UtcNow);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                authenticationType: "test"))
        };
        context.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();
        context.Request.Headers["X-Domain-Id"] = Guid.NewGuid().ToString();

        var accessor = new IdentityContextAccessor(
            new HttpContextAccessor { HttpContext = context },
            new StubTenantExecutionContextAccessor(executionContext));

        Assert.Equal(tenant.Id, accessor.GetCurrentTenantId());
        Assert.Equal(domainId, accessor.GetCurrentDomainId());
    }

    private sealed class StubTenantExecutionContextAccessor(TenantExecutionContext? current)
        : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current => current;
        public void Initialize(TenantExecutionContext context) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
    }
}
