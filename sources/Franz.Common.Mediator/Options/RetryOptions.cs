// Options
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Mediator.Options;
public sealed class RetryOptions
{
  public int MaxAttempts { get; set; } = 3;
  public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
  public Func<Exception, bool>? ShouldRetry { get; set; } = TransientExceptionDetector.Default;
  public Action<Exception, int, TimeSpan>? OnRetry { get; set; } = null;
  public Func<int, TimeSpan, TimeSpan>? ComputeDelay { get; set; } =
      (attempt, baseDelay) =>
      {
        // exponential backoff with jitter
        var exp = Math.Pow(2, attempt);
        var jitterMs = Random.Shared.Next(0, 100);
        return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * exp + jitterMs);
      };
}

// A simple transient detector you can extend per stack (HTTP/EF/Npgsql/etc.)
