using System.Security.Claims;
using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.Http.MultiTenancy.Middleware;
using Franz.Common.Http.MultiTenancy;
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
        var accessor = CreateExecutionAccessor(context);
        var called = false;
        var middleware = CreateMiddleware(tenant, accessor, new ServiceCollection().BuildServiceProvider());

        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(called);
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_ExecutesOnlyAfterMembershipAuthorization()
    {
        var context = CreateAuthenticatedContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = CreateExecutionAccessor(context);
        var services = new ServiceCollection()
            .AddSingleton<ITenantRequestAuthorizer, AllowTenantRequestAuthorizer>()
            .BuildServiceProvider();
        var called = false;
        var middleware = CreateMiddleware(tenant, accessor, services);

        await middleware.InvokeAsync(context, _ =>
        {
            called = true;
            Assert.Equal(tenant.Id, accessor.Current?.TenantId);
            return Task.CompletedTask;
        });

        Assert.True(called);
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_RejectsInactiveTenant()
    {
        var context = new DefaultHttpContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant") { Status = TenantStatus.Suspended };
        var accessor = CreateExecutionAccessor(context);
        var middleware = CreateMiddleware(tenant, accessor, new ServiceCollection().BuildServiceProvider());
        var called = false;

        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.False(called);
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_RejectsInvalidHostBeforeEstablishingExecutionContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("invalid/path");
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = CreateExecutionAccessor(context);
        var middleware = CreateMiddleware(tenant, accessor, new ServiceCollection().BuildServiceProvider());
        var called = false;

        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(called);
        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_RejectsMissingTenantWhenTenantIsRequired()
    {
        var context = new DefaultHttpContext();
        var accessor = CreateExecutionAccessor(context);
        var middleware = CreateMiddleware(null, accessor, new ServiceCollection().BuildServiceProvider());
        var called = false;

        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task Middleware_ClearsExecutionContextWhenDownstreamExecutionThrows()
    {
        var context = new DefaultHttpContext();
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = CreateExecutionAccessor(context);
        var middleware = CreateMiddleware(tenant, accessor, new ServiceCollection().BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, _ => throw new InvalidOperationException("downstream failure")));

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task Middleware_PropagatesRequestCancellationThroughResolutionValidationAndAuthorization()
    {
        using var cancellation = new CancellationTokenSource();
        var context = CreateAuthenticatedContext();
        context.RequestAborted = cancellation.Token;

        var tenant = new TenantInfo(Guid.NewGuid(), "tenant");
        var accessor = CreateExecutionAccessor(context);
        var pipeline = new CancellationAwareTenantResolutionPipeline(tenant);
        var validator = new CapturingTenantValidator();
        var authorizer = new CapturingTenantRequestAuthorizer();
        var services = new ServiceCollection()
            .AddSingleton<ITenantValidator>(validator)
            .AddSingleton<ITenantRequestAuthorizer>(authorizer)
            .BuildServiceProvider();
        var middleware = new TenantResolutionMiddleware(
            pipeline,
            accessor,
            Options.Create(new TenantResolutionOptions()),
            services);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(cancellation.Token, pipeline.CapturedToken);
        Assert.Equal(cancellation.Token, validator.CapturedToken);
        Assert.Equal(cancellation.Token, authorizer.CapturedToken);
        Assert.Null(accessor.Current);
    }

    private static DefaultHttpContext CreateAuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user")],
            "test"));
        return context;
    }

    private static TenantExecutionContextAccessor CreateExecutionAccessor(HttpContext context)
        => new(new HttpContextAccessor { HttpContext = context });

    private static TenantResolutionMiddleware CreateMiddleware(
        TenantInfo? tenant,
        ITenantExecutionContextAccessor accessor,
        IServiceProvider services)
    {
        var pipeline = new StubTenantResolutionPipeline(tenant);
        return new TenantResolutionMiddleware(
            pipeline,
            accessor,
            Options.Create(new TenantResolutionOptions()),
            services);
    }

    private sealed class StubTenantResolutionPipeline(TenantInfo? tenant) : ITenantResolutionPipeline
    {
        public Task<TenantResolutionResult?> ResolveAsync(object? context)
            => Task.FromResult(tenant is null
                ? null
                : TenantResolutionResult.Success(tenant, TenantResolutionSource.Host));
    }

    private sealed class CancellationAwareTenantResolutionPipeline(TenantInfo tenant) : ITenantResolutionPipeline
    {
        public CancellationToken CapturedToken { get; private set; }

        public Task<TenantResolutionResult?> ResolveAsync(object? context)
            => Task.FromResult<TenantResolutionResult?>(
                TenantResolutionResult.Success(tenant, TenantResolutionSource.Host));

        public Task<TenantResolutionResult?> ResolveAsync(object? context, CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            return ResolveAsync(context);
        }
    }

    private sealed class AllowTenantRequestAuthorizer : ITenantRequestAuthorizer
    {
        public Task<bool> IsAuthorizedAsync(
            ClaimsPrincipal principal,
            TenantInfo tenant,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class CapturingTenantValidator : ITenantValidator
    {
        public CancellationToken CapturedToken { get; private set; }

        public Task<bool> IsValidAsync(Guid tenantId) => Task.FromResult(true);

        public Task<bool> IsValidAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            return Task.FromResult(true);
        }
    }

    private sealed class CapturingTenantRequestAuthorizer : ITenantRequestAuthorizer
    {
        public CancellationToken CapturedToken { get; private set; }

        public Task<bool> IsAuthorizedAsync(
            ClaimsPrincipal principal,
            TenantInfo tenant,
            CancellationToken cancellationToken = default)
        {
            CapturedToken = cancellationToken;
            return Task.FromResult(true);
        }
    }
}
