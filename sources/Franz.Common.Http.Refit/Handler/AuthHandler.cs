#nullable enable
using Franz.Common.Http.Refit.Contracts;
using Franz.Common.Http.Refit.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Http.Refit.Handlers
{
  /// <summary>
  /// Delegating handler that injects an Authorization header using ITokenProvider,
  /// and optionally retries once after re-authentication on 401/403 responses.
  /// Automatically deactivates itself when no options or token provider are configured.
  /// </summary>
  public sealed class FranzRefitAuthHandler : DelegatingHandler
  {
    private readonly ITokenProvider? _tokenProvider;
    private readonly RefitClientOptions? _options;
    private readonly ILogger<FranzRefitAuthHandler> _logger;
    private bool _disabled;

    public FranzRefitAuthHandler(
      ITokenProvider? tokenProvider,
      RefitClientOptions? options,
      ILogger<FranzRefitAuthHandler> logger)
    {
      _tokenProvider = tokenProvider;
      _options = options;
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));

      if (_tokenProvider is null || _options is null)
      {
        _disabled = true;
        _logger.LogWarning("⚠️ FranzRefitAuthHandler disabled — no token provider or options configured.");
      }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      // 💤 Skip entirely if not configured
      if (_disabled)
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

      try
      {
        // Add bearer token if available
        var token = await _tokenProvider!.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
          request.Headers.Remove("Authorization");
          request.Headers.Add("Authorization", $"Bearer {token}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "⚠️ Token provider threw during GetTokenAsync — proceeding without auth header.");
      }

      var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

      // 🧩 Optional auto-retry logic
      if (_options!.AutoHandleAuthFailures &&
          (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden))
      {
        response.Dispose();
        _logger.LogWarning("🔄 Authentication failed ({StatusCode}) — retrying if configured.", response.StatusCode);

        for (int attempt = 1; attempt <= _options.MaxAuthRetryAttempts; attempt++)
        {
          try
          {
            if (_options.OnAuthenticationFailureAsync != null)
            {
              bool refreshed = await _options.OnAuthenticationFailureAsync.Invoke();
              if (refreshed)
              {
                _options.OnAuthRecoverySuccess?.Invoke();

                var newToken = await _tokenProvider!.GetTokenAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                  request.Headers.Remove("Authorization");
                  request.Headers.Add("Authorization", $"Bearer {newToken}");
                }

                _logger.LogInformation("✅ Token refreshed successfully, retrying request (Attempt {Attempt})", attempt);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
              }
            }
          }
          catch (Exception ex)
          {
            _options.OnAuthRecoveryFailed?.Invoke(ex);
            _logger.LogError(ex, "❌ Token refresh failed on attempt {Attempt}", attempt);
          }
        }

        _logger.LogError("🚫 Authentication retry exhausted after {MaxAttempts} attempts.", _options.MaxAuthRetryAttempts);
      }

      return response;
    }
  }
}
