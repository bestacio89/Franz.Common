using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Aras.Extensions.Options;
public class ArasInnovatorOptions
{
  /// <summary>Base URL of the Aras Innovator REST API.</summary>
  public string BaseUrl { get; set; } = default!;

  /// <summary>Authentication token (e.g., OAuth, basic, or Innovator token).</summary>
  public string? AuthToken { get; set; }

  /// <summary>Enable diagnostic decorators (logs, tracing).</summary>
  public bool UseDiagnostics { get; set; } = false;

  /// <summary>Snapshot frequency (every N events a snapshot is stored).</summary>
  public int SnapshotFrequency { get; set; } = 50;
}