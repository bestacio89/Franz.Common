using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Franz.Common.Integration.Tests.Mediator.Domain.Events;
/// <summary>
/// Lightweight test-only projection of any domain event.
/// Used by InMemoryProcessedEventSink to simplify assertions.
/// </summary>
public sealed record ProcessedEvent(string name, Guid id);
