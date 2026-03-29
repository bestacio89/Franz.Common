#nullable enable
using Franz.Common.Messaging.Delegating;
using Franz.Common.Messaging.Messages;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.Tests.Fakes;

/// <summary>
/// Fake handler for integration testing.
/// Senior Note: Updated to Task-based ProcessAsync to satisfy the refined IMessageHandler.
/// </summary>
public sealed class TestMessageHandler : IMessageHandler
{
  // Thread-static or volatile if running highly concurrent integration tests
  public static Message? LastMessage;

  public static readonly Guid TestCorrelationId = Guid.Parse("018e2f8a-9a91-7b3f-8e1f-4f2a3c4d5e6f");

  public Task ProcessAsync(Message message, CancellationToken ct = default)
  {
    if (message is null)
      return Task.CompletedTask;

    // ✅ Header enrichment using StringValues
    message.Headers["X-Test-Handled"] = new StringValues("true");

    // ✅ CorrelationId spine maintenance
    message.CorrelationId = TestCorrelationId;

    // Atomic reference assignment
    LastMessage = message;

    return Task.CompletedTask;
  }

  /// <summary>
  /// Helper to reset state between test runs
  /// </summary>
  public static void Reset()
  {
    LastMessage = null;
  }
}