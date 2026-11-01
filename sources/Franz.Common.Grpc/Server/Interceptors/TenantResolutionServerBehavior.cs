using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.Configuration;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.Extensions.Options;
using Grpc.Core;

namespace Franz.Common.Grpc.Server.Interceptors;

/// <summary>
/// Resolves a tenant from trusted server-side identity, authorizes membership,
/// establishes the validated execution context, and clears it after the call.
/// Caller-supplied tenant metadata is removed before resolution.
/// </summary>
public sealed class TenantResolutionServerBehavior<TRequest, TResponse>
    : IGrpcServerBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IFranzTenantResolver _tenantResolver;
    private readonly IFranzTenantAuthorizer? _tenantAuthorizer;
    private readonly ITenantExecutionContextAccessor? _tenantContextAccessor;
    private readonly ITenantValidator? _tenantValidator;
    private readonly FranzGrpcOptions _options;

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

    public async Task<TResponse> InvokeAsync(
        TRequest request,
        GrpcCallContext context,
        GrpcServerPipelineDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        // x-tenant-id is untrusted propagation metadata, never an authority on the server.
        var trustedContext = context.WithTenant(null).WithUser(null);

        // HTTP middleware may already have established and authorized the tenant for this
        // same ASP.NET request. Reuse that context and leave its cleanup to the HTTP scope.
        if (_tenantContextAccessor?.Current is { } establishedContext)
        {
            if (establishedContext.TenantId == Guid.Empty)
                throw new TenantIsolationException("The established gRPC tenant context is invalid.");

            return await next(
                request,
                trustedContext.WithTenant(establishedContext.TenantId.ToString("D")),
                cancellationToken).ConfigureAwait(false);
        }

        var resolvedTenantId = await _tenantResolver
            .ResolveTenantAsync(trustedContext, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(resolvedTenantId))
        {
            if (_options.RequireTenant)
                throw new RpcException(new Status(StatusCode.PermissionDenied, "A tenant is required for this gRPC operation."));

            return await next(request, trustedContext, cancellationToken).ConfigureAwait(false);
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

        try
        {
            var tenantContext = trustedContext.WithTenant(tenantId.ToString("D"));
            return await next(request, tenantContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _tenantContextAccessor.Clear();
        }
    }
}
