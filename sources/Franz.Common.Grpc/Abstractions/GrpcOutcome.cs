using System;
using System.Collections.Generic;
using Grpc.Core;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Represents the outcome of a gRPC call processed through the Franz pipeline.
/// This mirrors HttpOutcome and KafkaOutcome in design.
/// </summary>
public sealed record GrpcOutcome
{
  /// <summary>True if the operation succeeded.</summary>
  public bool IsSuccess { get; init; }

  /// <summary>Optional functional/business error code.</summary>
  public string? ErrorCode { get; init; }

  /// <summary>Logical error domain (Validation, Domain, Security, etc.).</summary>
  public string? ErrorDomain { get; init; }

  /// <summary>User-readable message.</summary>
  public string? Reason { get; init; }

  /// <summary>Underlying exception (only local, not serialized).</summary>
  public Exception? Exception { get; init; }

  /// <summary>Associated gRPC status code.</summary>
  public StatusCode StatusCode { get; init; }

  /// <summary>Metadata returned along the failure.</summary>
  public IDictionary<string, string>? ErrorMetadata { get; init; }

  private GrpcOutcome(
      bool isSuccess,
      StatusCode statusCode,
      string? errorCode = null,
      string? errorDomain = null,
      string? reason = null,
      Exception? exception = null,
      IDictionary<string, string>? metadata = null)
  {
    IsSuccess = isSuccess;
    StatusCode = statusCode;
    ErrorCode = errorCode;
    ErrorDomain = errorDomain;
    Reason = reason;
    Exception = exception;
    ErrorMetadata = metadata;
  }

  // ======================================================
  // SUCCESS
  // ======================================================

  public static GrpcOutcome Success() =>
      new(true, StatusCode.OK);

  // ======================================================
  // FAILURE FACTORIES
  // ======================================================

  public static GrpcOutcome Fail(
      StatusCode statusCode,
      string reason,
      string? errorCode = null,
      string? errorDomain = null,
      Exception? exception = null,
      IDictionary<string, string>? metadata = null)
  {
    return new GrpcOutcome(
        false,
        statusCode,
        errorCode,
        errorDomain,
        reason,
        exception,
        metadata);
  }

  public static GrpcOutcome Invalid(string reason, IDictionary<string, string>? metadata = null) =>
      Fail(StatusCode.InvalidArgument, reason, "INVALID_ARGUMENT", "Validation", metadata: metadata);

  public static GrpcOutcome Unauthorized(string reason) =>
      Fail(StatusCode.Unauthenticated, reason, "UNAUTHORIZED", "Security");

  public static GrpcOutcome Forbidden(string reason) =>
      Fail(StatusCode.PermissionDenied, reason, "FORBIDDEN", "Security");

  public static GrpcOutcome NotFound(string reason) =>
      Fail(StatusCode.NotFound, reason, "NOT_FOUND", "Domain");

  public static GrpcOutcome Conflict(string reason) =>
      Fail(StatusCode.FailedPrecondition, reason, "CONFLICT", "Domain");

  public static GrpcOutcome InternalError(string reason, Exception? ex = null) =>
      Fail(StatusCode.Internal, reason, "INTERNAL_ERROR", "Infrastructure", exception: ex);

  // ======================================================
  // EXCEPTION → OUTCOME
  // ======================================================

  /// <summary>
  /// Generic and transport-agnostic exception-to-outcome mapping.
  /// No dependencies on other Franz packages.
  /// </summary>
  public static GrpcOutcome FromException(Exception ex)
  {
    return ex switch
    {
      RpcException rpc => FromRpcException(rpc),

      ArgumentException a => Invalid(a.Message),
      InvalidOperationException i => Conflict(i.Message),

      // fallback → Infrastructure error
      _ => InternalError(ex.Message, ex),
    };
  }

  /// <summary>
  /// Converts gRPC RpcException into a structured outcome.
  /// </summary>
  public static GrpcOutcome FromRpcException(RpcException ex)
  {
    var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var t in ex.Trailers)
      metadata[t.Key] = t.Value ?? string.Empty;

    return new GrpcOutcome(
        isSuccess: false,
        statusCode: ex.StatusCode,
        errorCode: ex.StatusCode.ToString(),
        errorDomain: "RPC",
        reason: ex.Status.Detail,
        exception: ex,
        metadata: metadata
    );
  }

  // ======================================================
  // OUTCOME → RPC OBJECTS
  // ======================================================

  public Status ToStatus() => new(StatusCode, Reason ?? string.Empty);

  public RpcException ToRpcException()
  {
    var trailers = new Metadata();

    if (ErrorMetadata is not null)
    {
      foreach (var kvp in ErrorMetadata)
        trailers.Add(kvp.Key, kvp.Value);
    }

    if (ErrorCode is not null)
      trailers.Add("x-error-code", ErrorCode);

    if (ErrorDomain is not null)
      trailers.Add("x-error-domain", ErrorDomain);

    return new RpcException(ToStatus(), trailers);
  }

  public override string ToString()
  {
    return IsSuccess
        ? "GrpcOutcome => Success"
        : $"GrpcOutcome => Failure [{StatusCode}] Code='{ErrorCode}' Domain='{ErrorDomain}' Reason='{Reason}'";
  }
}
