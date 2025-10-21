using System;

namespace Franz.Common.Http.Refit.Options
{
  public class RefitClientOptions
  {
    /// <summary>
    /// Optional default timeout for the typed client.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Whether the Refit clients should add OTEL tags to Activity.Current.
    /// (Tracer/Exporter configuration remains responsibility of the host app.)
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// Optional default Polly policy name to pull from the shared policy registry.
    /// </summary>
    public string? DefaultPolicyName { get; set; }

    /// <summary>
    /// Whether to automatically handle authentication failures (401/403)
    /// by triggering the registered authentication refresh delegate, if available.
    /// </summary>
    public bool AutoHandleAuthFailures { get; set; } = true;

    /// <summary>
    /// The maximum number of re-authentication attempts allowed
    /// before the client propagates the exception.
    /// </summary>
    public int MaxAuthRetryAttempts { get; set; } = 1;

    /// <summary>
    /// Optional delegate invoked when an authentication failure is detected.
    /// Return true if the token or credentials were successfully refreshed.
    /// </summary>
    public Func<Task<bool>>? OnAuthenticationFailureAsync { get; set; }

    /// <summary>
    /// Optional delegate invoked after a successful authentication recovery.
    /// </summary>
    public Action? OnAuthRecoverySuccess { get; set; }

    /// <summary>
    /// Optional delegate invoked when authentication recovery permanently fails.
    /// </summary>
    public Action<Exception>? OnAuthRecoveryFailed { get; set; }
  }
}
