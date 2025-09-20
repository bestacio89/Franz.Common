#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Http.Refit.Handlers
{
  public interface ITokenProvider
  {
    /// <summary>
    /// Return bearer token (string) or null if not available.
    /// </summary>
    Task<string?> GetTokenAsync(CancellationToken ct = default);
  }

  /// <summary>
  /// Delegating handler that injects an Authorization: Bearer token if available.
  /// </summary>
  public sealed class FranzRefitAuthHandler : DelegatingHandler
  {
    private readonly ITokenProvider _tokenProvider;
    private readonly ILogger<FranzRefitAuthHandler> _logger;

    public FranzRefitAuthHandler(ITokenProvider tokenProvider, ILogger<FranzRefitAuthHandler> logger)
    {
      _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      var token = await _tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
      if (!string.IsNullOrEmpty(token))
      {
        request.Headers.Remove("Authorization");
        request.Headers.Add("Authorization", $"Bearer {token}");
      }

      return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
  }
}
