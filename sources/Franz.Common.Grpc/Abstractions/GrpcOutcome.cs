using System;
using System.Collections.Generic;
using System.Threading;
using Franz.Common.Errors;
using Grpc.Core;

namespace Franz.Common.Grpc.Abstractions;

/// <summary>
/// Represents the outcome of a gRPC call processed through the Franz pipeline.
/// </summary>
public sealed record GrpcOutcome
{
    private const string CorrelationIdTrailer = "x-correlation-id";
    private const string ErrorCodeTrailer = "x-error-code";
    private const string ErrorDomainTrailer = "x-error-domain";

    private static readonly HashSet<string> SafeTrailerKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        CorrelationIdTrailer,
        ErrorCodeTrailer,
        ErrorDomainTrailer
    };

    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorDomain { get; init; }
    public string? Reason { get; init; }
    public Exception? Exception { get; init; }
    public StatusCode StatusCode { get; init; }
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

    public static GrpcOutcome Success() => new(true, StatusCode.OK);

    public static GrpcOutcome Fail(
        StatusCode statusCode,
        string reason,
        string? errorCode = null,
        string? errorDomain = null,
        Exception? exception = null,
        IDictionary<string, string>? metadata = null)
        => new(false, statusCode, errorCode, errorDomain, reason, exception, metadata);

    public static GrpcOutcome Invalid(string reason, IDictionary<string, string>? metadata = null)
        => Fail(StatusCode.InvalidArgument, reason, "INVALID_ARGUMENT", "Validation", metadata: metadata);

    public static GrpcOutcome Unauthorized(string reason)
        => Fail(StatusCode.Unauthenticated, reason, "UNAUTHENTICATED", "Security");

    public static GrpcOutcome Forbidden(string reason)
        => Fail(StatusCode.PermissionDenied, reason, "PERMISSION_DENIED", "Security");

    public static GrpcOutcome NotFound(string reason)
        => Fail(StatusCode.NotFound, reason, "NOT_FOUND", "Domain");

    public static GrpcOutcome Conflict(string reason)
        => Fail(StatusCode.FailedPrecondition, reason, "FAILED_PRECONDITION", "Domain");

    public static GrpcOutcome InternalError(string reason, Exception? ex = null)
        => Fail(StatusCode.Internal, reason, "INTERNAL_ERROR", "Infrastructure", exception: ex);

    /// <summary>
    /// Maps known platform and Franz exceptions to stable gRPC status codes.
    /// Remote details are deliberately generic; the original exception remains
    /// available through <see cref="Exception"/> for trusted local diagnostics.
    /// </summary>
    public static GrpcOutcome FromException(
        Exception ex,
        CancellationToken callCancellationToken = default,
        DateTime? deadlineUtc = null,
        string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(ex);

        if (ex is RpcException rpcException)
            return FromRpcException(rpcException, correlationId);

        if (ex is OperationCanceledException)
        {
            if (deadlineUtc is { } deadline && deadline <= DateTime.UtcNow)
            {
                return MappedFailure(
                    StatusCode.DeadlineExceeded,
                    "The call deadline was exceeded.",
                    "DEADLINE_EXCEEDED",
                    "Cancellation",
                    ex,
                    correlationId);
            }

            if (callCancellationToken.IsCancellationRequested)
            {
                return MappedFailure(
                    StatusCode.Cancelled,
                    "The call was cancelled.",
                    "CANCELLED",
                    "Cancellation",
                    ex,
                    correlationId);
            }

            return InternalFailure(ex, correlationId);
        }

        return ex switch
        {
            UnauthorizedException => MappedFailure(
                StatusCode.Unauthenticated, "Authentication is required.",
                "UNAUTHENTICATED", "Security", ex, correlationId),

            UnauthorizedAccessException => MappedFailure(
                StatusCode.Unauthenticated, "Authentication is required.",
                "UNAUTHENTICATED", "Security", ex, correlationId),

            ForbiddenException => MappedFailure(
                StatusCode.PermissionDenied, "The caller is not authorized to perform this operation.",
                "PERMISSION_DENIED", "Security", ex, correlationId),

            NotFoundException => MappedFailure(
                StatusCode.NotFound, "The requested resource was not found.",
                "NOT_FOUND", "Domain", ex, correlationId),

            PreconditionFailedException => MappedFailure(
                StatusCode.FailedPrecondition, "The operation's preconditions were not met.",
                "FAILED_PRECONDITION", "Domain", ex, correlationId),

            BusinessException => MappedFailure(
                StatusCode.FailedPrecondition, "A business rule prevents this operation.",
                "BUSINESS_RULE_FAILED", "Domain", ex, correlationId),

            FunctionalException => MappedFailure(
                StatusCode.InvalidArgument, "The request is invalid.",
                "INVALID_ARGUMENT", "Validation", ex, correlationId),

            ArgumentException => MappedFailure(
                StatusCode.InvalidArgument, "The request is invalid.",
                "INVALID_ARGUMENT", "Validation", ex, correlationId),

            InvalidOperationException => MappedFailure(
                StatusCode.FailedPrecondition, "The operation cannot be completed in the current state.",
                "FAILED_PRECONDITION", "Domain", ex, correlationId),

            NotImplementedException => MappedFailure(
                StatusCode.Unimplemented, "The requested operation is not implemented.",
                "UNIMPLEMENTED", "Platform", ex, correlationId),

            _ => InternalFailure(ex, correlationId)
        };
    }

    /// <summary>
    /// Converts a downstream gRPC exception while retaining only safe status
    /// and allowlisted diagnostic trailers.
    /// </summary>
    public static GrpcOutcome FromRpcException(RpcException ex, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in ex.Trailers)
        {
            if (entry.IsBinary ||
                !SafeTrailerKeys.Contains(entry.Key) ||
                !IsSafeTrailerValue(entry.Value))
            {
                continue;
            }

            metadata[entry.Key] = entry.Value;
        }

        AddCorrelationId(metadata, correlationId);

        var statusCode = ex.StatusCode == StatusCode.OK
            ? StatusCode.Internal
            : ex.StatusCode;

        return new GrpcOutcome(
            isSuccess: false,
            statusCode: statusCode,
            errorCode: statusCode.ToString(),
            errorDomain: "RPC",
            reason: GetSafeReason(statusCode),
            exception: ex,
            metadata: metadata);
    }

    public Status ToStatus() => new(StatusCode, Reason ?? string.Empty);

    public RpcException ToRpcException()
    {
        var trailers = new Metadata();
        var addedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (ErrorMetadata is not null)
        {
            foreach (var (key, value) in ErrorMetadata)
                AddSafeTrailer(trailers, addedKeys, key, value);
        }

        if (ErrorCode is not null)
            AddSafeTrailer(trailers, addedKeys, ErrorCodeTrailer, ErrorCode);

        if (ErrorDomain is not null)
            AddSafeTrailer(trailers, addedKeys, ErrorDomainTrailer, ErrorDomain);

        return new RpcException(ToStatus(), trailers);
    }

    public override string ToString()
        => IsSuccess
            ? "GrpcOutcome => Success"
            : $"GrpcOutcome => Failure [{StatusCode}] Code='{ErrorCode}' Domain='{ErrorDomain}' Reason='{Reason}'";

    private static GrpcOutcome InternalFailure(Exception exception, string? correlationId)
        => MappedFailure(
            StatusCode.Internal,
            "An unexpected server error occurred.",
            "INTERNAL_ERROR",
            "Infrastructure",
            exception,
            correlationId);

    private static GrpcOutcome MappedFailure(
        StatusCode statusCode,
        string reason,
        string errorCode,
        string errorDomain,
        Exception exception,
        string? correlationId)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddCorrelationId(metadata, correlationId);

        return Fail(
            statusCode,
            reason,
            errorCode,
            errorDomain,
            exception,
            metadata);
    }

    private static void AddCorrelationId(IDictionary<string, string> metadata, string? correlationId)
    {
        if (IsSafeTrailerValue(correlationId))
            metadata[CorrelationIdTrailer] = correlationId!;
    }

    private static void AddSafeTrailer(
        Metadata trailers,
        ISet<string> addedKeys,
        string key,
        string value)
    {
        if (!SafeTrailerKeys.Contains(key) ||
            !IsSafeTrailerValue(value) ||
            !addedKeys.Add(key))
        {
            return;
        }

        trailers.Add(key, value);
    }

    private static bool IsSafeTrailerValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
            return false;

        foreach (var character in value)
        {
            if (!char.IsAscii(character) || char.IsControl(character))
                return false;
        }

        return true;
    }

    private static string GetSafeReason(StatusCode statusCode)
        => statusCode switch
        {
            StatusCode.Cancelled => "The call was cancelled.",
            StatusCode.InvalidArgument => "The request is invalid.",
            StatusCode.DeadlineExceeded => "The call deadline was exceeded.",
            StatusCode.NotFound => "The requested resource was not found.",
            StatusCode.AlreadyExists => "The requested resource already exists.",
            StatusCode.PermissionDenied => "The caller is not authorized to perform this operation.",
            StatusCode.ResourceExhausted => "The service cannot process the request at this time.",
            StatusCode.FailedPrecondition => "The operation cannot be completed in the current state.",
            StatusCode.Aborted => "The operation was aborted.",
            StatusCode.OutOfRange => "The requested value is outside the supported range.",
            StatusCode.Unauthenticated => "Authentication is required.",
            StatusCode.Unavailable => "The requested service is temporarily unavailable.",
            _ => "An unexpected server error occurred."
        };
}
