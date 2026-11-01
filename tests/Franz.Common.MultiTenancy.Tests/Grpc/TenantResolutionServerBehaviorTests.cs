using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.Client.Interceptors;
using Franz.Common.Grpc.Server.Interceptors;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Grpc.Core;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Franz.Common.MultiTenancy.Tests.Grpc;

public sealed class TenantResolutionServerBehaviorTests
{
    [Fact]
    public async Task InvokeAsync_RemovesCallerTenantAndEstablishesValidatedExecutionContext()
    {
        var tenantId = Guid.NewGuid();
        var resolver = Substitute.For<IFranzTenantResolver>();
        resolver.ResolveTenantAsync(Arg.Any<GrpcCallContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(tenantId.ToString("D")));

        var authorizer = Substitute.For<IFranzTenantAuthorizer>();
        authorizer.IsAuthorizedAsync(
                Arg.Any<GrpcCallContext>(), tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var accessor = new TestTenantExecutionContextAccessor();
        var validator = Substitute.For<ITenantValidator>();
        validator.IsValidAsync(tenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var behavior = new TenantResolutionServerBehavior<TestRequest, TestResponse>(
            resolver,
            Options.Create(new FranzGrpcOptions { RequireTenant = true }),
            authorizer,
            accessor,
            validator);

        var untrustedTenantId = Guid.NewGuid().ToString("D");
        var callContext = GrpcCallContext.CreateClient(
            "tests.Service", "Call", tenantId: untrustedTenantId, userId: "untrusted-user") with { IsServerSide = true };

        var invoked = false;
        await behavior.InvokeAsync(
            new TestRequest(),
            callContext,
            (request, context, _) =>
            {
                invoked = true;
                Assert.Equal(tenantId.ToString("D"), context.TenantId);
                Assert.NotEqual(untrustedTenantId, context.TenantId);
                Assert.Equal(tenantId, accessor.Current?.TenantId);
                return Task.FromResult(new TestResponse());
            });

        Assert.True(invoked);
        Assert.Null(accessor.Current);

        await resolver.Received(1).ResolveTenantAsync(
            Arg.Is<GrpcCallContext>(context =>
                context.TenantId is null &&
                context.UserId is null &&
                !context.RequestHeaders.ContainsKey(GrpcCallContext.TenantIdHeaderName) &&
                !context.RequestHeaders.ContainsKey(GrpcCallContext.UserIdHeaderName)),
            Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task InvokeAsync_ReusesHttpEstablishedTenantWithoutReplacingOrClearingIt()
    {
        var tenantId = Guid.NewGuid();
        var resolver = Substitute.For<IFranzTenantResolver>();
        var authorizer = Substitute.For<IFranzTenantAuthorizer>();
        var accessor = new TestTenantExecutionContextAccessor();
        accessor.Initialize(CreateTenantContext(tenantId));

        var behavior = new TenantResolutionServerBehavior<TestRequest, TestResponse>(
            resolver,
            Options.Create(new FranzGrpcOptions { RequireTenant = true }),
            authorizer,
            accessor);

        await behavior.InvokeAsync(
            new TestRequest(),
            GrpcCallContext.CreateClient(
                "tests.Service", "Call", tenantId: Guid.NewGuid().ToString("D")) with { IsServerSide = true },
            (_, context, _) =>
            {
                Assert.Equal(tenantId.ToString("D"), context.TenantId);
                Assert.Equal(tenantId, accessor.Current?.TenantId);
                return Task.FromResult(new TestResponse());
            });

        Assert.Equal(tenantId, accessor.Current?.TenantId);
        await resolver.DidNotReceive().ResolveTenantAsync(
            Arg.Any<GrpcCallContext>(), Arg.Any<CancellationToken>());
        await authorizer.DidNotReceive().IsAuthorizedAsync(
            Arg.Any<GrpcCallContext>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_ClearsTenantContextWhenHandlerThrows()
    {
        var tenantId = Guid.NewGuid();
        var resolver = Substitute.For<IFranzTenantResolver>();
        resolver.ResolveTenantAsync(Arg.Any<GrpcCallContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(tenantId.ToString("D")));

        var authorizer = Substitute.For<IFranzTenantAuthorizer>();
        authorizer.IsAuthorizedAsync(
                Arg.Any<GrpcCallContext>(), tenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var accessor = new TestTenantExecutionContextAccessor();
        var validator = Substitute.For<ITenantValidator>();
        validator.IsValidAsync(tenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var behavior = new TenantResolutionServerBehavior<TestRequest, TestResponse>(
            resolver,
            Options.Create(new FranzGrpcOptions { RequireTenant = true }),
            authorizer,
            accessor,
            validator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.InvokeAsync(
                new TestRequest(),
                GrpcCallContext.CreateClient("tests.Service", "Call") with { IsServerSide = true },
                (_, _, _) => throw new InvalidOperationException("handler failure")));

        Assert.Null(accessor.Current);
    }

    [Fact]
    public async Task InvokeAsync_RejectsMissingTenantWhenRequired()
    {
        var resolver = Substitute.For<IFranzTenantResolver>();
        resolver.ResolveTenantAsync(Arg.Any<GrpcCallContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        var behavior = new TenantResolutionServerBehavior<TestRequest, TestResponse>(
            resolver,
            Options.Create(new FranzGrpcOptions { RequireTenant = true }),
            Substitute.For<IFranzTenantAuthorizer>(),
            new TestTenantExecutionContextAccessor());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            behavior.InvokeAsync(
                new TestRequest(),
                GrpcCallContext.CreateClient("tests.Service", "Call") with { IsServerSide = true },
                (_, _, _) => Task.FromResult(new TestResponse())));

        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RejectsResolvedTenantWithoutMembershipAuthorizer()
    {
        var tenantId = Guid.NewGuid();
        var resolver = Substitute.For<IFranzTenantResolver>();
        resolver.ResolveTenantAsync(Arg.Any<GrpcCallContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(tenantId.ToString("D")));

        var behavior = new TenantResolutionServerBehavior<TestRequest, TestResponse>(
            resolver,
            Options.Create(new FranzGrpcOptions()),
            tenantAuthorizer: null,
            tenantContextAccessor: new TestTenantExecutionContextAccessor());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            behavior.InvokeAsync(
                new TestRequest(),
                GrpcCallContext.CreateClient("tests.Service", "Call") with { IsServerSide = true },
                (_, _, _) => Task.FromResult(new TestResponse())));

        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
    }


    [Fact]
    public async Task ClientBehavior_PropagatesOnlyValidatedExecutionTenant()
    {
        var tenantId = Guid.NewGuid();
        var accessor = new TestTenantExecutionContextAccessor();
        accessor.Initialize(CreateTenantContext(tenantId));
        var behavior = new TenantResolutionClientBehavior<TestRequest, TestResponse>(accessor);
        var spoofedTenantId = Guid.NewGuid().ToString("D");

        await behavior.InvokeAsync(
            new TestRequest(),
            GrpcCallContext.CreateClient("tests.Service", "Call", tenantId: spoofedTenantId),
            (_, context, _) =>
            {
                Assert.Equal(tenantId.ToString("D"), context.TenantId);
                return Task.FromResult(new TestResponse());
            });

        accessor.Clear();

        await behavior.InvokeAsync(
            new TestRequest(),
            GrpcCallContext.CreateClient("tests.Service", "Call", tenantId: spoofedTenantId),
            (_, context, _) =>
            {
                Assert.Null(context.TenantId);
                Assert.False(context.RequestHeaders.ContainsKey(GrpcCallContext.TenantIdHeaderName));
                return Task.FromResult(new TestResponse());
            });
    }

    private static TenantExecutionContext CreateTenantContext(Guid tenantId)
        => new(
            new TenantInfo(tenantId, tenantId.ToString("D")),
            domainId: null,
            TenantResolutionSource.Unknown,
            DateTimeOffset.UtcNow);

    private sealed class TestRequest { }
    private sealed class TestResponse { }

    private sealed class TestTenantExecutionContextAccessor : ITenantExecutionContextAccessor
    {
        public TenantExecutionContext? Current { get; private set; }

        public void Initialize(TenantExecutionContext context)
        {
            if (Current is not null)
                throw new TenantIsolationException("Context already initialized.");
            Current = context;
        }

        public void Clear() => Current = null;
    }
}
