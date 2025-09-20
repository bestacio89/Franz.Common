using Polly;
using System.Collections.Generic;

namespace Franz.Common.Mediator.Polly.Options
{
  public class PollyPolicyRegistryOptions
  {
    /// <summary>
    /// Collection of policies to register in the PolicyRegistry.
    /// The key is the policy name, the value is the policy itself.
    /// </summary>
    public IDictionary<string, IAsyncPolicy> Policies { get; }
        = new Dictionary<string, IAsyncPolicy>();
  }
}
