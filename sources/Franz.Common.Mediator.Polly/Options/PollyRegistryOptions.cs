using Polly;
using System.Collections.Generic;
using System.Net.Http;

namespace Franz.Common.Mediator.Polly.Options
{
  /// <summary>
  /// Holds both Mediator and HTTP resilience policies.
  /// Mediator policies are boxed (object) to support Result&lt;T&gt; of any type.
  /// </summary>
  public class PollyPolicyRegistryOptions
  {
    // --- Mediator (generic-boxed) ---
    public IDictionary<string, object> MediatorPolicies { get; }
        = new Dictionary<string, object>();

    // --- HTTP (typed) ---
    public IDictionary<string, IAsyncPolicy<HttpResponseMessage>> HttpPolicies { get; }
        = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
  }
}
