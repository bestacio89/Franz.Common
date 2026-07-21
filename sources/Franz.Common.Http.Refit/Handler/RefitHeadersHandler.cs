#nullable enable
using Franz.Common.Mediator.Context;
using Franz.Common.Mediator.Pipelines.Logging; // Access to CorrelationId.Ensure()
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;
using System.Net.Http;
using Franz.Common.Http.Refit.Metrics;

namespace Franz.Common.Http.Refit.Handlers;

/// <summary>
/// Hardened Refit handler. Propagates native Guid v7 correlation across network boundaries.
/// Maintains the "Golden Thread" in distributed tracing.
/// </summary>
public sealed class FranzRefitHeadersHandler : DelegatingHandler
{
  private readonly ILogger<FranzRefitHeadersHandler> _logger;

  public FranzRefitHeadersHandler(ILogger<FranzRefitHeadersHandler> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // BAZOOKA REFACTOR: Bridge the MediatorContext and the HTTP Headers.
    // We use the raw Guid to avoid unnecessary string allocations until the very last moment.
    var correlationGuid = MediatorContext.CorrelationId;
    MediatorContext.EnsureCorrelationId(); // Ensure a correlation ID is present in the MediatorContext
    var correlationId = correlationGuid.ToString(); // Use standard UUID format for HTTP headers

    // Ensure X-Correlation-ID is set (Propagating the Turbo ID)
    request.Headers.Remove("X-Correlation-ID");
    request.Headers.Add("X-Correlation-ID", correlationId);

    // Tenant Propagation
    if (MediatorContext.Current?.TenantId is { } tenantId)
    {
      request.Headers.Remove("X-Tenant-Id");
      request.Headers.Add("X-Tenant-Id", tenantId);
    }

    // User Identity Propagation
    if (MediatorContext.Current?.UserId is { } userId)
    {
      request.Headers.Remove("X-User-Id");
      request.Headers.Add("X-User-Id", userId);
    }

    var sw = Stopwatch.StartNew();
    try
    {
      // OpenTelemetry enrichment
      var activity = Activity.Current;
      activity?.SetTag("franz.http.outgoing", true);
      activity?.SetTag("franz.http.uri", request.RequestUri?.AbsolutePath);
      activity?.SetTag("franz.correlation_id", correlationId);

      RefitMetrics.Requests.Add(1);

      var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

      sw.Stop();
      RefitMetrics.DurationMs.Record(sw.Elapsed.TotalMilliseconds);

      activity?.SetTag("franz.http.status_code", (int)response.StatusCode);
      activity?.SetTag("franz.http.elapsed_ms", sw.Elapsed.TotalMilliseconds);

      // Log with the native Guid for Serilog efficiency
      using (LogContext.PushProperty("FranzCorrelationId", correlationGuid))
      using (LogContext.PushProperty("FranzHttpMethod", request.Method.Method))
      using (LogContext.PushProperty("FranzHttpPath", request.RequestUri?.AbsolutePath))
      {
        _logger.LogInformation("HTTP → {Method} {Path} responded {Status} in {Elapsed}ms",
            request.Method, request.RequestUri?.AbsolutePath, (int)response.StatusCode, sw.Elapsed.TotalMilliseconds);
      }

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();
      RefitMetrics.Failures.Add(1);

      var activity = Activity.Current;
      activity?.SetTag("franz.http.error", true);
      activity?.SetTag("franz.http.elapsed_ms", sw.Elapsed.TotalMilliseconds);

      using (LogContext.PushProperty("FranzCorrelationId", correlationGuid))
      using (LogContext.PushProperty("FranzHttpMethod", request.Method.Method))
      using (LogContext.PushProperty("FranzHttpPath", request.RequestUri?.AbsolutePath))
      {
        _logger.LogError(ex, "HTTP → {Method} {Path} failed after {Elapsed}ms",
            request.Method, request.RequestUri?.AbsolutePath, sw.Elapsed.TotalMilliseconds);
      }

      throw;
    }
  }
}