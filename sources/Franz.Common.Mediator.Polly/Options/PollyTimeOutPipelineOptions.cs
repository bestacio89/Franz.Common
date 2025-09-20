using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Polly.Options;
/// <summary>
/// Options for the Polly Timeout pipeline.
/// </summary>
public class PollyTimeoutPipelineOptions
{
  /// <summary>
  /// The name of the policy to resolve from the registry.
  /// </summary>
  public string PolicyName { get; set; } = string.Empty;
}