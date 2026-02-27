using Franz.Common.Business.Events;
using Franz.Common.Mediator.Messages;
using System;

namespace Franz.Common.EntityFramework.Tests.Repositories.Fakes;



public class DummyEvent : IDomainEvent
{
  // Identifiant de l'agrégat (souvent passé au constructeur en situation réelle)
  public Guid? AggregateId { get; set; }

  public string AggregateType { get; set; } = "DummyAggregate";

  // Utilisation de GuidV7 pour la corrélation (permet de suivre le flux temporel)
  public Guid? CorrelationId { get; set; } = Guid.CreateVersion7();

  // L'identifiant unique de l'événement en version 7
  public Guid EventId { get; set; } = Guid.CreateVersion7();

  public string EventType { get; set; } = nameof(DummyEvent);

  // Date précise de l'événement
  public DateTimeOffset OccurredOn { get; set; } = DateTimeOffset.UtcNow;
}