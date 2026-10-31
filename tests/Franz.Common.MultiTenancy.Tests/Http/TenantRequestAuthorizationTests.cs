using System.Security.Claims;
using Franz.Common.Http.MultiTenancy;
using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.Http.MultiTenancy.Middleware;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Franz.Common.MultiTenancy.TenantResolution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class TenantRequestAuthorizationTests
{
    [Fact]
    public async Task Middleware_RejectsAuthenticatedPrincipalWhenMembershipAuthorizerIsMissing()
    {
        var context = CreateAuthenticatedContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = new TenantExecutionContextAccessor(new HttpContextAccessor { HttpContext = context });
        var called = false;
        var middleware = CreateMiddleware(tenant, accessor, new ServiceCollection().BuildServiceProvider(), () => called = true);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(called);
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_ExecutesOnlyAfterMembershipAuthorization()
    {
        var context = CreateAuthenticatedContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = new TenantExecutionContextAccessor(new HttpContextAccessor { HttpContext = context });
        var services = new ServiceCollection()
            .AddSingleton<ITenantRequestAuthorizer, AllowTenantRequestAuthorizer>()
            .BuildServiceProvider();
        var called = false;
        var middleware = CreateMiddleware(tenant, accessor, services, () =>
        {
            called = true;
            Assert.Equal(tenant.Id, accessor.Current?.TenantId);
        });

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Null(accessor.Current);
    }

    private static DefaultHttpContext CreateAuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "test"));
        return context;
    }

    private static TenantResolutionMiddleware CreateMiddleware(
        TenantInfo tenant,
        ITenantExecutionContextAccessor accessor,
        IServiceProvider services,
        Action next)
    {
        var pipeline = new StubTenantResolutionPipeline(tenant);
        return new TenantResolutionMiddleware(
            _ => { next(); return Task.CompletedTask; },
            pipeline,
            accessor,
            Options.Create(new TenantResolutionOptions()),
            services);
    }

    private sealed class StubTenantResolutionPipeline(TenantInfo tenant) : ITenantResolutionPipeline
    {
        public Task<TenantResolutionResult?> ResolveAsync(object? context)
            => Task.FromResult<TenantResolutionResult?>(TenantResolutionResult.Success(tenant, TenantResolutionSource.Host));
    }

    private sealed class AllowTenantRequestAuthorizer : ITenantRequestAuthorizer
    {
        public Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, TenantInfo tenant, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
