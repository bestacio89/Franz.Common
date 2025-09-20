#nullable enable
using Franz.Common.Mediator.Context;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Http.Refit.Metrics;

namespace Franz.Common.Http.Refit.Handlers
{
  /// <summary>
  /// Adds correlation/tenant/user headers to outgoing HTTP calls and enriches Activity/metrics/logging.
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
      // Ensure correlation ID
      var correlationId = MediatorContext.Current?.CorrelationId ?? Guid.NewGuid().ToString("N");
      if (request.Headers.Contains("X-Correlation-ID"))
        request.Headers.Remove("X-Correlation-ID");
      request.Headers.Add("X-Correlation-ID", correlationId);

      // Tenant
      if (!string.IsNullOrEmpty(MediatorContext.Current?.TenantId))
      {
        if (request.Headers.Contains("X-Tenant-Id"))
          request.Headers.Remove("X-Tenant-Id");
        request.Headers.Add("X-Tenant-Id", MediatorContext.Current.TenantId!);
      }

      // Optional user id
      if (!string.IsNullOrEmpty(MediatorContext.Current?.UserId))
      {
        if (request.Headers.Contains("X-User-Id"))
          request.Headers.Remove("X-User-Id");
        request.Headers.Add("X-User-Id", MediatorContext.Current.UserId!);
      }

      var sw = Stopwatch.StartNew();
      try
      {
        Activity.Current?.SetTag("franz.http.outgoing", true);
        Activity.Current?.SetTag("franz.http.uri", request.RequestUri?.AbsolutePath);

        RefitMetrics.Requests.Add(1);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        sw.Stop();
        RefitMetrics.DurationMs.Record(sw.Elapsed.TotalMilliseconds);
        Activity.Current?.SetTag("franz.http.status_code", (int)response.StatusCode);
        Activity.Current?.SetTag("franz.http.elapsed_ms", sw.Elapsed.TotalMilliseconds);

        using (LogContext.PushProperty("FranzCorrelationId", correlationId))
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
        Activity.Current?.SetTag("franz.http.error", true);
        Activity.Current?.SetTag("franz.http.elapsed_ms", sw.Elapsed.TotalMilliseconds);

        using (LogContext.PushProperty("FranzCorrelationId", correlationId))
        using (LogContext.PushProperty("FranzHttpMethod", request.Method.Method))
        using (LogContext.PushProperty("FranzHttpPath", request.RequestUri?.AbsolutePath))
        {
          _logger.LogError(ex, "HTTP → {Method} {Path} failed after {Elapsed}ms", request.Method, request.RequestUri?.AbsolutePath, sw.Elapsed.TotalMilliseconds);
        }

        throw;
      }
    }
  }
}
