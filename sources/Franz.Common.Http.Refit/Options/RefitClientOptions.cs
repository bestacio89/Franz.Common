using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Http.Refit.Options;

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
  }


