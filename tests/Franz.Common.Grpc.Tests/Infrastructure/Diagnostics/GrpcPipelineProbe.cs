using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Tests.Infrastructure.Diagnostics;

/// <summary>
/// Captures identity and tenant observations at distinct points in the gRPC pipeline.
/// </summary>
internal sealed class GrpcPipelineProbe : IFranzTenantResolver, IFranzTenantAuthorizer
{
    public const string ContextMarkerHeaderName = "x-franz-test-pipeline-context";

    private int _invocationCount;
    private int _authorizationInvocationCount;
    private int _tenantResolutionInvocationCount;
    private int _tenantAuthorizationInvocationCount;
    private int _serverStreamingBehaviorInvocationCount;
    private int _clientStreamingBehaviorInvocationCount;
    private int _duplexStreamingBehaviorInvocationCount;
    private string? _lastUserId;
    private string? _lastAuthorizedUserId;
    private string? _lastServiceUserId;
    private string? _lastServiceTenantId;
    private string? _lastServiceContextMarker;
    private string? _lastRequestedTenantId;
    private string? _lastRequestedTenantHeader;
    private string? _lastAuthorizedTenantId;

    public int InvocationCount => Volatile.Read(ref _invocationCount);
    public int AuthorizationInvocationCount => Volatile.Read(ref _authorizationInvocationCount);
    public int TenantResolutionInvocationCount => Volatile.Read(ref _tenantResolutionInvocationCount);
    public int TenantAuthorizationInvocationCount => Volatile.Read(ref _tenantAuthorizationInvocationCount);
    public int ServerStreamingBehaviorInvocationCount => Volatile.Read(ref _serverStreamingBehaviorInvocationCount);
    public int ClientStreamingBehaviorInvocationCount => Volatile.Read(ref _clientStreamingBehaviorInvocationCount);
    public int DuplexStreamingBehaviorInvocationCount => Volatile.Read(ref _duplexStreamingBehaviorInvocationCount);
    public string? LastUserId => Volatile.Read(ref _lastUserId);
    public string? LastAuthorizedUserId => Volatile.Read(ref _lastAuthorizedUserId);
    public string? LastServiceUserId => Volatile.Read(ref _lastServiceUserId);
    public string? LastServiceTenantId => Volatile.Read(ref _lastServiceTenantId);
    public string? LastServiceContextMarker => Volatile.Read(ref _lastServiceContextMarker);
    public string? LastRequestedTenantId => Volatile.Read(ref _lastRequestedTenantId);
    public string? LastRequestedTenantHeader => Volatile.Read(ref _lastRequestedTenantHeader);
    public Guid? LastAuthorizedTenantId =>
        Guid.TryParse(Volatile.Read(ref _lastAuthorizedTenantId), out var tenantId) ? tenantId : null;

    public Task<string?> ResolveTenantAsync(GrpcCallContext context, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _tenantResolutionInvocationCount);
        Volatile.Write(ref _lastRequestedTenantId, context.TenantId);
        context.RequestHeaders.TryGetValue(GrpcCallContext.TenantIdHeaderName, out var selector);
        Volatile.Write(ref _lastRequestedTenantHeader, selector);
        return Task.FromResult<string?>(Guid.TryParse(selector, out _) ? selector : null);
    }

    public Task<bool> IsAuthorizedAsync(
        GrpcCallContext context,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _tenantAuthorizationInvocationCount);
        Volatile.Write(ref _lastAuthorizedTenantId, tenantId.ToString("D"));
        return Task.FromResult(true);
    }

    public void Record(string? userId)
    {
        Volatile.Write(ref _lastUserId, userId);
        Interlocked.Increment(ref _invocationCount);
    }

    public void RecordServerStreamingBehaviorInvocation() => Interlocked.Increment(ref _serverStreamingBehaviorInvocationCount);
    public void RecordClientStreamingBehaviorInvocation() => Interlocked.Increment(ref _clientStreamingBehaviorInvocationCount);
    public void RecordDuplexStreamingBehaviorInvocation() => Interlocked.Increment(ref _duplexStreamingBehaviorInvocationCount);

    public void RecordAuthorization(string? userId)
    {
        Volatile.Write(ref _lastAuthorizedUserId, userId);
        Interlocked.Increment(ref _authorizationInvocationCount);
    }

    public void RecordServiceContext(GrpcCallContext context)
    {
        Volatile.Write(ref _lastServiceUserId, context.UserId);
        Volatile.Write(ref _lastServiceTenantId, context.TenantId);
        context.RequestHeaders.TryGetValue(ContextMarkerHeaderName, out var marker);
        Volatile.Write(ref _lastServiceContextMarker, marker);
    }
}
