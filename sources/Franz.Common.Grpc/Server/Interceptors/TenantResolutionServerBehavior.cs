using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Configuration;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Resolves a tenant from trusted server-side identity, authorizes membership,
/// establishes the validated execution context, and clears it after the complete RPC lifetime.
/// Caller-supplied tenant metadata is only a resolver selection hint.
/// </summary>
public sealed class TenantResolutionServerBehavior<TRequest, TResponse> :
    IGrpcServerBehavior<TRequest, TResponse>,
    IGrpcServerStreamingBehavior<TRequest, TResponse>,
    IGrpcClientStreamingBehavior<TRequest, TResponse>,
    IGrpcDuplexStreamingBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzTenantResolver _tenantResolver;
    private readonly IFranzTenantAuthorizer? _tenantAuthorizer;
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;
    private readonly ITenantValidator? _tenantValidator;
    private readonly FranzGrpcOptions _options;

    public TenantResolutionServerBehavior(IFranzTenantResolver tenantResolver)
        : this(tenantResolver, Options.Create(new FranzGrpcOptions()))
    {
    }

    public TenantResolutionServerBehavior(
        IFranzTenantResolver tenantResolver,
        IOptions<FranzGrpcOptions> options,
        IFranzTenantAuthorizer? tenantAuthorizer = null,
        ITenantExecutionContextAccessor? tenantContextAccessor = null,
        ITenantValidator? tenantValidator = null)
    {
        _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tenantAuthorizer = tenantAuthorizer;
        _tenantContextAccessor = tenantContextAccessor;
        _tenantValidator = tenantValidator;
    }

    public int Order => GrpcServerBehaviorOrder.TenantResolution;

    public async Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var resolution = await ResolveTenantContextAsync(context, cancellationToken).ConfigureAwait(false);

        try
        {
            return await next(request, resolution.Context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ClearTenantContext(resolution);
        }
    }

    async IAsyncEnumerable<TResponse> IGrpcServerStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var resolution = await ResolveTenantContextAsync(context, cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (var response in next(request, resolution.Context, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return response;
            }
        }
        finally
        {
            ClearTenantContext(resolution);
        }
    }

    async Task<TResponse> IGrpcClientStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcClientStreamingPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var resolution = await ResolveTenantContextAsync(context, cancellationToken).ConfigureAwait(false);

        try
        {
            return await next(requestStream, resolution.Context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ClearTenantContext(resolution);
        }
    }

    async IAsyncEnumerable<TResponse> IGrpcDuplexStreamingBehavior<TRequest, TResponse>.InvokeAsync(
        IAsyncEnumerable<TRequest> requestStream,
        GrpcCallContext context,
        GrpcDuplexStreamingPipelineDelegate<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestStream);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var resolution = await ResolveTenantContextAsync(context, cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (var response in next(requestStream, resolution.Context, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return response;
            }
        }
        finally
        {
            ClearTenantContext(resolution);
        }
    }

    private async Task<TenantResolutionResult> ResolveTenantContextAsync(
        GrpcCallContext context,
        CancellationToken cancellationToken)
    {
        var resolutionContext = context.WithoutUserHeader();
        var trustedContext = resolutionContext.WithTenant(null);

        if (_tenantContextAccessor?.Current is { } establishedContext)
        {
            if (establishedContext.TenantId == Guid.Empty)
                throw new TenantIsolationException("The established gRPC tenant context is invalid.");

            return new TenantResolutionResult(
                trustedContext.WithTenant(establishedContext.TenantId.ToString("D")),
                OwnsExecutionContext: false);
        }

        var resolvedTenantId = await _tenantResolver
            .ResolveTenantAsync(resolutionContext, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            if (_options.RequireTenant)
                throw new RpcException(new Status(StatusCode.PermissionDenied, "A tenant is required for this gRPC operation."));

            return new TenantResolutionResult(trustedContext, OwnsExecutionContext: false);
        }

        if (!Guid.TryParse(resolvedTenantId, out var tenantId) || tenantId == Guid.Empty)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The resolved tenant identifier is invalid."));

        if (_tenantAuthorizer is null ||
            !await _tenantAuthorizer.IsAuthorizedAsync(trustedContext, tenantId, cancellationToken).ConfigureAwait(false))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The caller is not authorized for the resolved tenant."));
        }

        if (_options.RequireTenant && _tenantValidator is null)
            throw new InvalidOperationException(
                "ITenantValidator must be registered when gRPC tenant enforcement is required.");

        if (_tenantValidator is not null &&
            !await _tenantValidator.IsValidAsync(tenantId, cancellationToken).ConfigureAwait(false))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The resolved tenant is not active or valid."));
        }

        if (_tenantContextAccessor is null)
            throw new InvalidOperationException(
                "A tenant execution context accessor must be registered when gRPC tenant resolution is enabled.");

        if (_tenantContextAccessor.Current is not null)
            throw new TenantIsolationException(
                "A gRPC call cannot replace an existing tenant execution context.");

        var tenant = new TenantInfo(tenantId, tenantId.ToString("D"));
        var executionContext = new TenantExecutionContext(
            tenant,
            domainId: null,
            TenantResolutionSource.Grpc,
            DateTimeOffset.UtcNow);

        _tenantContextAccessor.Initialize(executionContext);

        return new TenantResolutionResult(
            trustedContext.WithTenant(tenantId.ToString("D")),
            OwnsExecutionContext: true);
    }

    private void ClearTenantContext(TenantResolutionResult resolution)
    {
        if (resolution.OwnsExecutionContext)
            _tenantContextAccessor!.Clear();
    }

    private readonly record struct TenantResolutionResult(
        GrpcCallContext Context,
        bool OwnsExecutionContext);
}
