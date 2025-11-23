using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Grpc.Core;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Represents the logical context of a gRPC call in Franz.
/// Wraps correlation, tenant, user and metadata information in an immutable structure
/// that can be used on both client and server side.
/// </summary>
public sealed record GrpcCallContext
{
  // Standard Franz header names (kept internal to avoid string duplication elsewhere for now)
  public const string CorrelationIdHeaderName = "x-correlation-id";
  public const string RequestIdHeaderName = "x-request-id";
  public const string TenantIdHeaderName = "x-tenant-id";
  public const string UserIdHeaderName = "x-user-id";

  /// <summary>Logical correlation id used to link a chain of calls.</summary>
  public string CorrelationId { get; init; }

  /// <summary>Logical request id unique for the current boundary (API, worker, etc.).</summary>
  public string RequestId { get; init; }

  /// <summary>Optional tenant identifier when running multi-tenant.</summary>
  public string? TenantId { get; init; }

  /// <summary>Optional user identifier (subject).</summary>
  public string? UserId { get; init; }

  /// <summary>Fully-qualified gRPC method name (e.g. /package.Service/Operation).</summary>
  public string Method { get; init; }

  /// <summary>Name of the service type handling the call (server) or being called (client).</summary>
  public string ServiceName { get; init; }

  /// <summary>Remote peer (ip:port) when available.</summary>
  public string? Peer { get; init; }

  /// <summary>Call deadline in UTC, if any.</summary>
  public DateTime? DeadlineUtc { get; init; }

  /// <summary>Cancellation token associated with the call.</summary>
  public CancellationToken CancellationToken { get; init; }

  /// <summary>Request metadata as an immutable, case-insensitive map.</summary>
  public IImmutableDictionary<string, string> RequestHeaders { get; init; }

  /// <summary>Response metadata as an immutable, case-insensitive map.</summary>
  public IImmutableDictionary<string, string> ResponseHeaders { get; init; }

  /// <summary>Indicates whether this context was created on the server side.</summary>
  public bool IsServerSide { get; init; }

  /// <summary>Indicates whether this context was created on the client side.</summary>
  public bool IsClientSide => !IsServerSide;

  private static readonly StringComparer HeaderComparer = StringComparer.OrdinalIgnoreCase;

  private GrpcCallContext(
      string correlationId,
      string requestId,
      string? tenantId,
      string? userId,
      string method,
      string serviceName,
      string? peer,
      DateTime? deadlineUtc,
      CancellationToken cancellationToken,
      IImmutableDictionary<string, string> requestHeaders,
      IImmutableDictionary<string, string> responseHeaders,
      bool isServerSide)
  {
    CorrelationId = correlationId;
    RequestId = requestId;
    TenantId = tenantId;
    UserId = userId;
    Method = method;
    ServiceName = serviceName;
    Peer = peer;
    DeadlineUtc = deadlineUtc;
    CancellationToken = cancellationToken;
    RequestHeaders = requestHeaders;
    ResponseHeaders = responseHeaders;
    IsServerSide = isServerSide;
  }

  #region Factory methods

  /// <summary>
  /// Creates a context for an outgoing client call.
  /// </summary>
  public static GrpcCallContext CreateClient(
      string serviceName,
      string methodName,
      string? correlationId = null,
      string? requestId = null,
      string? tenantId = null,
      string? userId = null,
      DateTime? deadlineUtc = null,
      CancellationToken cancellationToken = default,
      IEnumerable<KeyValuePair<string, string>>? headers = null)
  {
    correlationId ??= Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    requestId ??= Guid.NewGuid().ToString("N");

    var method = NormalizeMethodName(serviceName, methodName);

    var headerDict = ToImmutableHeaderDictionary(headers)
        .SetItem(CorrelationIdHeaderName, correlationId)
        .SetItem(RequestIdHeaderName, requestId);

    if (!string.IsNullOrWhiteSpace(tenantId))
      headerDict = headerDict.SetItem(TenantIdHeaderName, tenantId);

    if (!string.IsNullOrWhiteSpace(userId))
      headerDict = headerDict.SetItem(UserIdHeaderName, userId);

    return new GrpcCallContext(
        correlationId,
        requestId,
        tenantId,
        userId,
        method,
        serviceName,
        peer: null,
        deadlineUtc,
        cancellationToken,
        headerDict,
        ImmutableDictionary<string, string>.Empty.WithComparers(HeaderComparer),
        isServerSide: false);
  }

  /// <summary>
  /// Creates a context from a server-side <see cref="ServerCallContext"/>.
  /// </summary>
  public static GrpcCallContext FromServerCallContext(
      ServerCallContext serverContext,
      string? tenantId = null,
      string? userId = null)
  {
    if (serverContext is null)
      throw new ArgumentNullException(nameof(serverContext));

    var requestHeaders = ToImmutableHeaderDictionary(serverContext.RequestHeaders
        .Select(h => new KeyValuePair<string, string>(h.Key, h.Value)));

    // Try to reuse correlation/request ids if already present
    var correlationId = GetOrCreate(requestHeaders, CorrelationIdHeaderName);
    var requestId = GetOrCreate(requestHeaders, RequestIdHeaderName);

    tenantId ??= requestHeaders.TryGetValue(TenantIdHeaderName, out var t) ? t : null;
    userId ??= requestHeaders.TryGetValue(UserIdHeaderName, out var u) ? u : null;

    var method = serverContext.Method ?? string.Empty;
    var serviceName = ExtractServiceName(method);

    DateTime? deadlineUtc = serverContext.Deadline == DateTime.MaxValue
    ? (DateTime?)null
    : serverContext.Deadline.ToUniversalTime();

    return new GrpcCallContext(
        correlationId,
        requestId,
        tenantId,
        userId,
        method,
        serviceName,
        serverContext.Peer,
        deadlineUtc,
        serverContext.CancellationToken,
        requestHeaders,
        ImmutableDictionary<string, string>.Empty.WithComparers(HeaderComparer),
        isServerSide: true);
  }

  #endregion

  #region Metadata helpers

  /// <summary>
  /// Converts the request headers into a <see cref="Metadata"/> instance
  /// suitable for attaching to outgoing calls.
  /// </summary>
  public Metadata ToRequestMetadata()
  {
    var metadata = new Metadata();
    foreach (var kvp in RequestHeaders)
    {
      metadata.Add(kvp.Key, kvp.Value);
    }

    return metadata;
  }

  /// <summary>
  /// Creates <see cref="CallOptions"/> for an outgoing client call,
  /// using this context as the source of cancellation, deadline and metadata.
  /// </summary>
  public CallOptions ToCallOptions(CallOptions? existing = null)
  {
    var options = existing ?? default;

    // Clone existing headers
    var headers = new Metadata();
    if (options.Headers is not null)
    {
      foreach (var h in options.Headers)
        headers.Add(h.Key, h.Value);
    }

    // Add missing headers from RequestHeaders
    foreach (var kvp in RequestHeaders)
    {
      if (!headers.Any(h => string.Equals(h.Key, kvp.Key, StringComparison.OrdinalIgnoreCase)))
      {
        headers.Add(kvp.Key, kvp.Value);
      }
    }

    options = options.WithHeaders(headers);

    // Apply deadline if present
    if (DeadlineUtc is not null)
    {
      options = options.WithDeadline(DeadlineUtc.Value);
    }

    // Apply cancellation token
    if (CancellationToken != default)
    {
      options = options.WithCancellationToken(CancellationToken);
    }

    return options;
  }


  #endregion

  #region With* helpers

  public GrpcCallContext WithTenant(string? tenantId) =>
      this with
      {
        TenantId = tenantId,
        RequestHeaders = UpdateHeader(TenantIdHeaderName, tenantId)
      };

  public GrpcCallContext WithUser(string? userId) =>
      this with
      {
        UserId = userId,
        RequestHeaders = UpdateHeader(UserIdHeaderName, userId)
      };

  public GrpcCallContext WithDeadline(DateTime? deadlineUtc) =>
      this with { DeadlineUtc = deadlineUtc };

  public GrpcCallContext WithCancellation(CancellationToken token) =>
      this with { CancellationToken = token };

  public GrpcCallContext WithAdditionalHeader(string name, string value) =>
      this with { RequestHeaders = RequestHeaders.SetItem(name, value) };

  #endregion

  #region Internal helpers

  private static string NormalizeMethodName(string serviceName, string methodName)
  {
    if (string.IsNullOrWhiteSpace(serviceName))
      return methodName ?? string.Empty;

    if (methodName.StartsWith("/", StringComparison.Ordinal))
      return methodName;

    // Convention: /{serviceName}/{methodName}
    return serviceName.StartsWith("/", StringComparison.Ordinal)
        ? $"{serviceName.TrimEnd('/')}/{methodName}"
        : $"/{serviceName}/{methodName}";
  }

  private static string ExtractServiceName(string method)
  {
    // /package.Service/Method -> package.Service
    if (string.IsNullOrWhiteSpace(method))
      return string.Empty;

    var trimmed = method.TrimStart('/');
    var index = trimmed.IndexOf('/', StringComparison.Ordinal);
    return index < 0 ? trimmed : trimmed[..index];
  }

  private static IImmutableDictionary<string, string> ToImmutableHeaderDictionary(
      IEnumerable<KeyValuePair<string, string>>? headers)
  {
    var builder = ImmutableDictionary.CreateBuilder<string, string>(HeaderComparer);

    if (headers is null)
      return builder.ToImmutable();

    foreach (var kvp in headers)
    {
      // last write wins
      builder[kvp.Key] = kvp.Value;
    }

    return builder.ToImmutable();
  }

  private static string GetOrCreate(
      IImmutableDictionary<string, string> headers,
      string key)
  {
    if (headers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
      return value;

    return Guid.NewGuid().ToString("N");
  }

  private IImmutableDictionary<string, string> UpdateHeader(string name, string? value)
  {
    var dict = RequestHeaders;

    if (string.IsNullOrWhiteSpace(value))
    {
      return dict.Remove(name);
    }

    return dict.SetItem(name, value);
  }

  #endregion
}
