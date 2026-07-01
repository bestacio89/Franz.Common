using Franz.Common.Errors;
using Franz.Common.Mediator.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Franz.Common.Http.Bootstrap.Exceptions;

/// <summary>
/// Production-grade global exception handler for all Franz-based HTTP services.
///
/// Registered automatically via AddHttpArchitecture() — no per-service
/// configuration required. Any Franz service using Franz.Common.Http.Bootstrap
/// gets this handler for free.
///
/// Design decisions:
///
/// 1. NEVER LEAKS STACK TRACES — Detail only carries exception.Message in
///    development. In production, a sanitized reference string is returned.
///    Stack traces never reach the client — debugging happens via structured logs.
///
/// 2. FRANZ EXCEPTION HIERARCHY — maps TechnicalException, BusinessException,
///    and ValidationException to semantically correct HTTP status codes.
///    Services that throw the right Franz exception get the right response code
///    automatically with no per-controller error handling required.
///
/// 3. ALWAYS RETURNS TRUE — terminal handler. Returning false would propagate
///    to the default ASP.NET error page, never correct for a JSON API.
///
/// 4. CORRELATION PROPAGATION — TraceId from HttpContext is included in every
///    ProblemDetails response so callers can correlate their error report with
///    the structured log entry without any implementation detail being exposed.
///
/// 5. ENVIRONMENT-AWARE DETAIL — full message in Development, sanitized
///    reference in Production/Staging. IHostEnvironment injected, not read
///    from a static or hardcoded string.
/// </summary>
public sealed class FranzGlobalExceptionHandler : IExceptionHandler
{
  private readonly ILogger<FranzGlobalExceptionHandler> _logger;
  private readonly IHostEnvironment _environment;

  public FranzGlobalExceptionHandler(
      ILogger<FranzGlobalExceptionHandler> logger,
      IHostEnvironment environment)
  {
    _logger = logger;
    _environment = environment;
  }

  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    var (statusCode, title) = Classify(exception);

    _logger.LogError(
        exception,
        "[{TraceId}] Unhandled {ExceptionType} on {Method} {Path} → {StatusCode} {Title}",
        httpContext.TraceIdentifier,
        exception.GetType().Name,
        httpContext.Request.Method,
        httpContext.Request.Path,
        (int)statusCode,
        title);

    var problemDetails = new ProblemDetails
    {
      Status = (int)statusCode,
      Title = title,
      Detail = SanitizeDetail(exception),
      Instance = httpContext.Request.Path,
    };

    problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

    httpContext.Response.StatusCode = (int)statusCode;
    httpContext.Response.ContentType = "application/problem+json";

    await httpContext.Response
        .WriteAsJsonAsync(problemDetails, cancellationToken)
        .ConfigureAwait(false);

    return true;
  }

  // =========================================================
  // CLASSIFICATION
  //
  // Franz hierarchy:
  //   TechnicalException  → 500  infrastructure fault
  //   BusinessException   → 422  domain rule violation
  //   ValidationException → 400  malformed input
  //
  // Standard .NET:
  //   ArgumentException family    → 400
  //   InvalidOperationException   → 422
  //   UnauthorizedAccessException → 401
  //   NotImplementedException     → 501
  //   OperationCanceledException  → 499 (client disconnected)
  //   Everything else             → 500
  // =========================================================
  private static (HttpStatusCode StatusCode, string Title) Classify(Exception exception)
    => exception switch
    {
      NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),

      ForbiddenException => (HttpStatusCode.Forbidden, "Access denied"),

      PreconditionFailedException => (HttpStatusCode.PreconditionFailed, "Precondition failed"),

      FunctionalException => (HttpStatusCode.UnprocessableEntity, "Business rule violated"),

      ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),

      ArgumentException => (HttpStatusCode.BadRequest, "Invalid request"),

      InvalidOperationException => (HttpStatusCode.UnprocessableEntity, "Invalid operation"),

      UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),

      NotImplementedException => (HttpStatusCode.NotImplemented, "Not implemented"),

      OperationCanceledException => ((HttpStatusCode)499, "Request cancelled"),

      _ => (HttpStatusCode.InternalServerError, "Unexpected error")
    };

  private string SanitizeDetail(Exception exception)
      => _environment.IsDevelopment()
          ? exception.Message
          : "An error occurred processing your request. " +
            $"Reference: {System.Diagnostics.Activity.Current?.TraceId}";
}