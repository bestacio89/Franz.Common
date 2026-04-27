#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;

public sealed class TestMessageHandler : IMessageHandler
{
  // Thread-safe enough for integration tests
  public static Message? LastMessage;

  public static readonly Guid TestCorrelationId =
      Guid.Parse("018e2f8a-9a91-7b3f-8e1f-4f2a3c4d5e6f");

  public Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    if (message is null)
      return Task.CompletedTask;

    // -----------------------------
    // Header enrichment (null-safe)
    // -----------------------------
    message.Headers["X-Test-Handled"] = new[] { "true" };

    // -----------------------------
    // Correlation enforcement
    // -----------------------------
    message.CorrelationId = TestCorrelationId;

    // -----------------------------
    // Capture for assertions
    // -----------------------------
    LastMessage = message;

    return Task.CompletedTask;
  }

  /// <summary>
  /// Reset state between tests
  /// </summary>
  public static void Reset()
  {
    LastMessage = null;
  }
}