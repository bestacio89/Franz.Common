using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Polly.Options;
/// <summary>
/// Options for the Polly Retry pipeline.
/// </summary>
public class PollyRetryPipelineOptions
{
  /// <summary>
  /// The name of the policy to resolve from the registry.
  /// </summary>
  public string PolicyName { get; set; } = string.Empty;
}