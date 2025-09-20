using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Polly.Options;
/// <summary>
/// Options for configuring an Advanced Circuit Breaker policy.
/// </summary>
public class PollyAdvancedCircuitBreakerOptions
{
  /// <summary>
  /// Optional policy name for lookup in the registry.
  /// </summary>
  public string PolicyName { get; set; } = "DefaultAdvancedBreaker";
}