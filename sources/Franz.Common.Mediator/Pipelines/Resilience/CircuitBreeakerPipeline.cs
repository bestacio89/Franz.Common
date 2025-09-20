using Franz.Common.Mediator.Options;
using Franz.Common.Mediator.Pipelines.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Mediator.Pipelines.Resilience
{
  public class CircuitBreakerPipeline<TRequest, TResponse> : IPipeline<TRequest, TResponse>
      where TRequest : notnull
  {
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<TRequest> _logger;
    private readonly IHostEnvironment _env;

    private int _failureCount = 0;
    private DateTime? _circuitOpened = null;
    private readonly SemaphoreSlim _halfOpenSemaphore = new SemaphoreSlim(1, 1);
    private readonly object _stateLock = new();

    public CircuitBreakerPipeline(CircuitBreakerOptions options, ILogger<TRequest> logger, IHostEnvironment env)
    {
      if (options.FailureThreshold <= 0)
        throw new ArgumentOutOfRangeException(nameof(options.FailureThreshold), "FailureThreshold must be greater than zero.");
      if (options.OpenDuration <= TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(options.OpenDuration), "OpenDuration must be greater than zero.");

      _options = options;
      _logger = logger;
      _env = env;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
      if (_options.Disabled)
      {
        _logger.LogInformation("CircuitBreakerPipeline disabled for {Request}", typeof(TRequest).Name);
        return await next();
      }

      // --- State Check ---
      var circuitState = GetCircuitState();

      if (circuitState == CircuitState.Open)
      {
        _logger.LogWarning("Circuit OPEN for {Request}", typeof(TRequest).Name);
        throw new InvalidOperationException($"Circuit breaker is OPEN for {typeof(TRequest).Name}, try again later.");
      }
      else if (circuitState == CircuitState.HalfOpen)
      {
        // Only one thread at a time can test the half-open state.
        if (!await _halfOpenSemaphore.WaitAsync(0, cancellationToken))
        {
          _logger.LogWarning("Circuit HALF-OPEN for {Request}, rejecting queued requests", typeof(TRequest).Name);
          throw new InvalidOperationException($"Circuit breaker is HALF-OPEN for {typeof(TRequest).Name}, try again later.");
        }
      }

      // --- Execution ---
      try
      {
        var response = await next();

        // If a request succeeds in the half-open state, reset the circuit.
        if (circuitState == CircuitState.HalfOpen)
        {
          ResetCircuit();
          _logger.LogInformation("Circuit reset for {Request}", typeof(TRequest).Name);
        }

        return response;
      }
      catch (Exception ex)
      {
        // If the request was in a half-open state and it failed, open the circuit again.
        if (circuitState == CircuitState.HalfOpen)
        {
          OpenCircuit(ex);
          _logger.LogWarning("Circuit failed in HALF-OPEN state for {Request}", typeof(TRequest).Name);
        }
        else // If it was in a closed state, increment the failure counter.
        {
          _failureCount++;
          if (_failureCount >= _options.FailureThreshold)
          {
            OpenCircuit(ex);
          }
          else
          {
            _logger.LogWarning(ex, "Failure {Count}/{Threshold} for {Request}", _failureCount, _options.FailureThreshold, typeof(TRequest).Name);
          }
        }
        throw;
      }
      finally
      {
        if (circuitState == CircuitState.HalfOpen)
        {
          _halfOpenSemaphore.Release();
        }
      }
    }

    // --- Helper Methods to manage state ---
    private void ResetCircuit()
    {
      lock (_stateLock)
      {
        _failureCount = 0;
        _circuitOpened = null;
      }
    }

    private void OpenCircuit(Exception ex)
    {
      lock (_stateLock)
      {
        _circuitOpened = DateTime.UtcNow;
        _logger.LogError(ex, "Circuit opened for {Request}. Will remain open for {Duration}s", typeof(TRequest).Name, _options.OpenDuration.TotalSeconds);
      }
    }

    private CircuitState GetCircuitState()
    {
      lock (_stateLock)
      {
        if (_circuitOpened.HasValue)
        {
          if (DateTime.UtcNow >= _circuitOpened.Value.Add(_options.OpenDuration))
          {
            return CircuitState.HalfOpen;
          }
          return CircuitState.Open;
        }
        return CircuitState.Closed;
      }
    }
  }

  // An enum to represent the states of the circuit breaker.
  public enum CircuitState
  {
    Closed,
    Open,
    HalfOpen
  }
}